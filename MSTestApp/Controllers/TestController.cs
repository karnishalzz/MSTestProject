using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MSTestApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace MSTestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        
        private readonly ITestService _service;

        public TestController(ILogger<TestController> logger,ITestService testService )
        {
            _service = testService;
        }
        
        
        [HttpPost]
        public async Task<ActionResult> GetData(List<TestModel> data)
        {
            await _service.ProcessData(data);
            return Ok(data);
        }
    }
}
