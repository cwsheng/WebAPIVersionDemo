using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIVersionDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //Deprecated=true:表示v1即将作废
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0")]
    [Route("/api/v{version:apiVersion}/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            //获取版本
            string v = HttpContext.GetRequestedApiVersion().ToString();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = $"v{v}:{Summaries[rng.Next(Summaries.Length)]}"
            })
            .ToArray();
        }

        [HttpGet]
        [MapToApiVersion("3.0")]
        public IEnumerable<WeatherForecast> GetV3()
        {
            //获取版本
            string v = HttpContext.GetRequestedApiVersion().ToString();
            var rng = new Random();
            return Enumerable.Range(1, 1).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = $"v{v}:{Summaries[rng.Next(Summaries.Length)]}"
            })
            .ToArray();
        }
    }
}
