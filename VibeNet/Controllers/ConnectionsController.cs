using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using System;

namespace VibeNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionsController : ControllerBase
    {
    }
}
//POST / send - request

//POST / accept - request

//POST / decline - request

//POST / ignore

//GET /{ userId}/ pending

//GET /{ userId}/ accepted

//GET /{ userId}/ ignored

//DELETE /{ userId}/ remove /{ otherUserId}