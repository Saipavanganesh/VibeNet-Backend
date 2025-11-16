using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;

namespace VibeNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
    }
}
//POST / start

//GET /{ userId} (list chats for that user)

//POST /{chatId}/ messages

//GET /{ chatId}/ messages