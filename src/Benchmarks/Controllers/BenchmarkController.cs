using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Benchmarks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BenchmarkController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            //var r = new Random();
            // assign some SOH memory
            //var x = new char[1024];
            
            // assign some LOH memory
            //x = new char[1024 * 100];

            //var result = CompileMe(() => r.Next());
            
            return new string[] {"value1" };
        }

        private int CompileMe(Expression<Func<int>> func)
        {
            return func.Compile()();
        }
    }
}