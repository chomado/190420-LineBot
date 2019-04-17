﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CEK.CSharp.Models;
using CEK.CSharp;
using Line.Messaging;
using Microsoft.Extensions.Configuration;

namespace EchoBot.Controllers
{
    [Route("api/clova")]
    public class ClovaController : Controller
    {
        private ClovaClient _client;
        private IConfiguration _configuration;

        public ClovaController()
        {
            _client = new ClovaClient();
        }

        public ClovaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            CEKRequest request = await _client.GetRequest(
                Request.Headers["SignatureCEK"],
                Request.Body
            );

            var response = new CEKResponse();

            switch (request.Request.Type)
            {
                case RequestType.LaunchRequest:
                    {
                        response.AddText("こんにちは！これ食べれるニャンボットです。人間の食べ物を入力すると、これは猫が食べて大丈夫かどうかを答えるよ");
                        response.ShouldEndSession = false;
                        break;
                    }
                case RequestType.SessionEndedRequest:
                    {
                        response.AddText("またね～");
                        break;
                    }
                case RequestType.IntentRequest:
                    {
                        switch (request.Request.Intent.Name)
                        {
                            case "CatIntent": // "パン"
                                {
                                    var food = request.Request.Intent.Slots.FirstOrDefault().Value.Value;
                                    // 食べれるものが渡ってくるので何か処理をする TODO!
                                    response.AddText(text: $"{food}の話ですか？");

                                    // LINE messaging に push
                                    string accessToken = _configuration.GetValue<string>("LINEMessaginAPIKey");

                                    var messagingClient = new LineMessagingClient(channelAccessToken: accessToken);
                                    await messagingClient.PushMessageAsync(
                                        to: request.Session.User.UserId,
                                        messages: new List<ISendMessage>
                                        {
                                            new TextMessage($"{food}"),
                                        }
                                    );
                                    response.ShouldEndSession = false;
                                    break;
                                }

                            case "ChomadoIntent": // "ちょまど"
                                {
                                    response.AddText("ちょまどさん、さすがオトナです！素敵！すばらしい！松屋！");
                                    response.ShouldEndSession = false;
                                    break;
                                }
                            case "Clova.GuideIntent":
                                {
                                    response.AddText("パン、と言ってみてください。");
                                    response.ShouldEndSession = false;
                                    break;
                                }
                        }
                        break;
                    }
            }
            return new OkObjectResult(response);
        }

    }
}