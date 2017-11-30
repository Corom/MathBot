using Microsoft.Cognitive.LUIS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Adventure_Works.CognitiveServices.LUIS
{
    class LUISIntentHandlers
    {
        [IntentHandler(0.4, Name = "greet")]
        public async Task<bool> Greet(LuisResult result, object context)
        {
            LUISIntentStatus usingIntentRouter = (LUISIntentStatus)context;

            usingIntentRouter.Success = true;
            return true;
        }

        [IntentHandler(0.4, Name = "thanks")]
        public async Task<bool> HandleThanks(LuisResult result, object context)
        {
            LUISIntentStatus usingIntentRouter = (LUISIntentStatus)context;
            usingIntentRouter.SpeechRespose = "your welcome";
            usingIntentRouter.Success = true;
            return true;
        }

        [IntentHandler(0.4, Name = "Math")]
        public async Task<bool> DoMath(LuisResult result, object context)
        {
            LUISIntentStatus usingIntentRouter = (LUISIntentStatus)context;
            usingIntentRouter.SpeechRespose = "I like math too";
            usingIntentRouter.Success = true;
            return true;
        }
        

        [IntentHandler(0.65, Name = "None")]
        public async Task<bool> HandleNone(LuisResult result, object context)
        {
            LUISIntentStatus usingIntentRouter = (LUISIntentStatus)context;
            usingIntentRouter.Success = false;
            return true;
        }
    }
}
