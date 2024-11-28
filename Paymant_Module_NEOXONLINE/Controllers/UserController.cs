using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Identity;
using Payment.Domain.DTOs;
using Payment.Domain.ECommerce;
using Payment.Domain.Identity;
using Payment.Domain.PayProduct;
using System.Net;

namespace Paymant_Module_NEOXONLINE.Controllers
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public UserController(ITokenService tokenService, IUnitOfWork unitOfWork)
        {
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets info about all users in db
        /// </summary> 
        /// <response code="200">Returns info about all categories in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _unitOfWork.GetRepository<User>()
            .AsQueryable()
            .Include(u => u.Basket)
            .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Gets info about certain user in db
        /// </summary> 
        /// <param name="username">First name of user to get information about</param>
        /// <response code="200">Returns info about certain user</response>
        /// <response code="404">User with such username not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetUserByFirstName")]
        public async Task<IActionResult> GetUserByFirstName(Guid guidIdUser)
        {
            var user = await _unitOfWork.GetRepository<User>().AsQueryable().Where(u => u.Id.Equals(guidIdUser)).Include(u => u.Basket).FirstOrDefaultAsync();
            if (user != null)
            {
                return Ok(user);
            }
            else
            {
                return NotFound($"user with id {guidIdUser} not found ");
            }
        }

        /// <summary>
        /// Creates user in db
        /// </summary> 
        /// <response code="200">Returns info about created user</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromQuery] UserCreationDto userCreationDto)
        {
            // Проверка на обязательные поля
            if (string.IsNullOrEmpty(userCreationDto.FirstName))
            {
                return BadRequest("First name is required.");
            }

            if (string.IsNullOrEmpty(userCreationDto.LastName))
            {
                return BadRequest("Last name is required.");
            }

            if (string.IsNullOrEmpty(userCreationDto.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrEmpty(userCreationDto.Сountry))
            {
                return BadRequest("Country is required.");
            }

            if (string.IsNullOrEmpty(userCreationDto.Address))
            {
                return BadRequest("Address is required.");
            }

            if (string.IsNullOrEmpty(userCreationDto.PhoneNumber))
            {
                return BadRequest("Phone number is required.");
            }

            // Создание нового пользователя
            var newUser = new User
            {
                FirstName = userCreationDto.FirstName,
                LastName = userCreationDto.LastName,
                Email = userCreationDto.Email,
                Сountry = userCreationDto.Сountry,
                Address = userCreationDto.Address,
                PhoneNumber = userCreationDto.PhoneNumber,
            };

            // Создание нового объекта корзины для пользователя
            _unitOfWork.GetRepository<User>().Create(newUser);
            _unitOfWork.GetRepository<Basket>().Create(new Basket() { User = newUser });

            // Сохранение изменений в базе данных
            await _unitOfWork.SaveShangesAsync();

            return Ok(newUser);
        }

        /// <summary>
        /// Creates user in db by jwt-token
        /// </summary> 
        /// <param name="token"> A JSON Web Token (JWT) used to identify the user</param> 
        /// <response code="200">Returns info about created user</response>
        /// <response code="400">Token is invalid</response>
        /// <response code="500">Server error</response>
        /// <example>
        /// Example of an encrypted token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJQYXltYW50X01vZHVsZV9ORU9YT05MSU5FIiwiYXVkIjoiUGF5bWFudF9Nb2R1bGVfTkVPWE9OTElORSIsImlhdCI6MTczMDk5OTk5OSwiZXhwIjoxNzM0OTk5OTk5LCJpZCI6IjY3OUI4NTZGLUE3NUMtNDg5RC1BOTYxLUNBMDU5MTc5MUIwQyIsInByZWZlcnJlZF91c2VybmFtZSI6IkpvaG4gRG9lIiwiZW1haWwiOiJqb2huLmRvZUBleGFtcGxlLmV4YW1wbGUiLCJjb3VudHJ5IjoiVVNBIiwiYWRkcmVzcyI6InNvbWUgYWRyZXNzIiwicGhvbmVOdW1iZXIiOiIxMTExMTExIn0.MLjkn0msLTd01FiQi3H3muMSgJR4l1tCcqSAmearFkw".
        /// Example of a decoded token payload:
        /// {
        ///  "iss": "Paymant_Module_NEOXONLINE",
        ///  "aud": "Paymant_Module_NEOXONLINE",
        ///  "iat": 1730999999,
        ///  "exp":1734999999,
        ///  "id" : "679B856F-A75C-489D-A961-CA0591791B0C",
        ///  "preferred_username": "John Doe",
        ///  "email" : "john.doe@example.example",
        ///  "country" : "USA",
        ///  "address" : "some adress",
        ///  "phoneNumber" : "1111111"
        ///}
        /// Example of signature:6225EF32-7D49-43AA-99DC-461C207CC6A9
        /// </example>
        [HttpPost("CreateUserByToken")]
        public async Task<IActionResult> CreateUserByToken(string token)
        {
            try
            {
                var claimsList = _tokenService.DecryptToken(token);
                var user = _tokenService.GetUser(claimsList);

                _unitOfWork.GetRepository<User>().Create(user);
                _unitOfWork.GetRepository<Basket>().Create(new Basket() { User = user });

                await _unitOfWork.SaveShangesAsync();
                return Ok(user);
            }
            catch (SecurityTokenException ex)
            {
                return BadRequest("Token is invalid");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest("Token does not contain reguired fields");
            }
        }

        /// <summary>
        /// Updates info about certain user in db
        /// </summary> 
        /// <response code="200">Returns info about updated user</response>
        /// <response code="404">User with such username not found</response>
        /// <response code="500">Server error</response>
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromQuery] Guid guidIdUser,[FromForm] UserCreationDto userDto)
        {
            // Извлечение пользователя из базы данных
            var user = await _unitOfWork.GetRepository<User>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(u => u.Id.Equals(guidIdUser));

            if (user != null)
            {
                // Возвращаем текущие данные пользователя ДО обновления
                var userDataBeforeUpdate = new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Сountry,
                    user.Address,
                    user.PhoneNumber
                };

                // Теперь выполняем обновление данных пользователя
                if (!string.IsNullOrEmpty(userDto.FirstName))
                {
                    user.FirstName = userDto.FirstName;
                }
                if (!string.IsNullOrEmpty(userDto.LastName))
                {
                    user.LastName = userDto.LastName;
                }
                if (!string.IsNullOrEmpty(userDto.Email))
                {
                    user.Email = userDto.Email;
                }
                if (!string.IsNullOrEmpty(userDto.Сountry))
                {
                    user.Сountry = userDto.Сountry;
                }
                if (!string.IsNullOrEmpty(userDto.Address))
                {
                    user.Address = userDto.Address;
                }
                if (!string.IsNullOrEmpty(userDto.PhoneNumber))
                {
                    user.PhoneNumber = userDto.PhoneNumber;
                }

                // Сохранение изменений в базе данных
                _unitOfWork.GetRepository<User>().Update(user);
                await _unitOfWork.SaveShangesAsync();

                // Возвращаем данные пользователя ДО и ПОСЛЕ обновления
                return Ok(new
                {
                    message = "User updated successfully",
                    userDataBeforeUpdate,  // Данные до обновления
                    updatedData = user     // Обновленные данные
                });
            }
            else
            {
                return NotFound($"User with id {guidIdUser} not found");
            }
        }

        /// <summary>
        /// Deletes certain user from db
        /// </summary> 
        /// <response code="200">User deleted successfully</response>
        /// <response code="404">User with such username not found</response>
        /// <response code="500">Server error</response>
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var deletedUser = await _unitOfWork.GetRepository<User>()
                .AsQueryable()
                .FirstOrDefaultAsync(u => u.Id.Equals(userId));
            if (deletedUser == null)
            {
                return NotFound(new { message = $"Invalid source data. Not Found {userId}" });
            }
            else
            {
                _unitOfWork.GetRepository<User>().Delete(deletedUser);
                await _unitOfWork.SaveShangesAsync();
                return Ok($"{deletedUser} has been deleted");
            }
        }
    }
}
