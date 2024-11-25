using Microsoft.AspNetCore.Mvc;

namespace Paymant_Module_NEOXONLINE.Controllers.PayPalC
{
    [Route("billing/swagger/api/[controller]")]
    public class PayPalPageController : Controller
    {
        // Метод для отображения страницы "htmlpage.html" с использованием абсолютного пути
        [HttpGet("index")]
        public IActionResult Index()
        { 
            // Получение абсолютного пути к файлу "htmlpage.html"
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pages", "htmlpage.html");

            // Проверка существования файла перед его отправкой
            if (!System.IO.File.Exists(rootPath))
            {
                return NotFound("HTML page not found.");
            }

            // Отправляем HTML файл с MIME-типом "text/html"
            return PhysicalFile(rootPath, "text/html");
        }

        // Метод для получения содержимого HTML-страницы в формате текста
        [HttpGet("get-html")]
        public async Task<IActionResult> GetHtmlPage()
        { 
            // Получение абсолютного пути к файлу "htmlpage.html"
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pages", "htmlpage.html");

            // Проверка существования файла перед его чтением
            if (!System.IO.File.Exists(rootPath))
            {
                return NotFound("HTML page not found.");
            }

            // Чтение содержимого HTML-файла
            string htmlContent = await System.IO.File.ReadAllTextAsync(rootPath);

            // Возвращаем содержимое HTML-файла с правильным MIME-типом
            return Content(htmlContent, "text/html");
        }
    }
}
