using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

[Serializable]
[QnAMakerService("41841226e856477dbb51a4e5f2d0b8e0", "2fbea3cc-2c2c-47cc-a53e-87f5eb7ed5e1")]
public class FaqDialog : QnAMakerDialog<object> {
    public override async Task NoMatchHandler(IDialogContext context, string originalQueryText) {
        await context.PostAsync($"Sorry, I couldn't find an answer for '{originalQueryText}'.");
        context.Wait(MessageReceived);
    }

    public override async Task DefaultMatchHandler(IDialogContext context, string originalQueryText, QnAMakerResult result) {
        // ProcessResultAndCreateMessageActivity will remove any attachment markup from the results answer
        // and add any attachments to a new message activity with the message activity text set by default
        // to the answer property from the result
        var messageActivity = ProcessResultAndCreateMessageActivity(context, ref result);
        messageActivity.Text = $"I found an answer that might help...{result.Answer}.";

        await context.PostAsync(messageActivity);

        context.Wait(MessageReceived);
    }

    [QnAMakerResponseHandler(100)]
    public async Task HighScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result) {
        await context.PostAsync($"{result.Answer}.");
        // attachment
        // <attachment contentType="video/mp4" contentUrl="http://www.yourdomain.com/video.mp4" name="Your title" thumbnailUrl="http://www.yourdomain.com/thumbnail.png" />
        context.Wait(MessageReceived);
    }

    [QnAMakerResponseHandler(50)]
    public async Task LowScoreHandler(IDialogContext context, string originalQueryText, QnAMakerResult result) {
        await context.PostAsync($"I found an answer that might help...{result.Answer}.");
        context.Wait(MessageReceived);
    }
}