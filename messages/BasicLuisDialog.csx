using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json;

// For more information about this template visit http://aka.ms/azurebots-csharp-luis
[Serializable]
public class BasicLuisDialog : LuisDialog<object>


{
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
        FemaleIdentifiers = new List<string>
        {
            "mother",
            "mom",
            "grandmother",
            "girlfriend",
            "sister",
            "daughter",
            "girl"
        };
        MaleIdentifiers = new List<string>
        {
            "father",
            "dad",
            "brother",
            "son",
            "boy",
            "boyfriend"
        };
    }

    public const string PersonEntityKey = "Person";
    public const string AgeEntityKey = "builtin.age";
    public const string GenderEntityKey = "Gender";
    public const string InterestsEntityKey = "Interests";

    public const string GenderMale = "Male";
    public const string GenderFemale = "Female";

    public List<string> FemaleIdentifiers;
    public List<string> MaleIdentifiers;

    [LuisIntent("None")]
    public async Task NoneIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the none intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    // Go to https://luis.ai and create a new intent, then train/publish your luis app.
    // Finally replace "MyIntent" with the name of your newly created intent in the following handler
    [LuisIntent("GetInspiration")]
    public async Task GetInspiration(IDialogContext context, LuisResult result)
    {

        //Check hvilke entiteter der er identificeret.
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);


        //var entities = result.Entities;
        //var person = entities.FirstOrDefault(x => x.Type.Equals("Person"));
        //if (person != null && person.Score > 0.60) {
        //    context.UserData.SetValue("Person", person.Entity);
        //    await context.PostAsync($"How old is {person.Entity} {JsonConvert.SerializeObject(result)}?"); //
        //    context.Wait(MessageReceived);
        //}
        //else {
        //    await context.PostAsync($"Who do you want to buy a gift for? {JsonConvert.SerializeObject(result)}"); //
        //    context.Wait(MessageReceived);
        //}
    }

    [LuisIntent("IdentifyPerson")]
    public async Task IdentifyPerson(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        string person;
        if (TryGetConversationData(context, PersonEntityKey, out person))
        {
            await ProceedInspirationConversation(context, result);
        }
        else
        {
            await context.PostAsync($"I am not clever enough to understand you?"); //
        }
        context.Wait(MessageReceived);
    }

    [LuisIntent("IdentifyGender")]
    public async Task IdentifySex(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }

    [LuisIntent("IdentifyAge")]
    public async Task IdentifyAge(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }

    [LuisIntent("IdentifyInterests")]
    public async Task IdentifyInterests(IDialogContext context, LuisResult result)
    {
        FetchInspirationData(context, result);
        await ProceedInspirationConversation(context, result);
    }



    private async Task ProceedInspirationConversation(IDialogContext context, LuisResult result)
    {
        string person;
        if (!TryGetConversationData(context, PersonEntityKey, out person))
        {
            await context.PostAsync($"Who do you want to buy a gift for? {JsonConvert.SerializeObject(context.ConversationData)}"); //
        }
        else if (!HasConversationData(context, GenderEntityKey))
        {
            await context.PostAsync($"Is {person} male or female? {JsonConvert.SerializeObject(result)}");
        }
        else if (!HasConversationData(context, AgeEntityKey))
        {
            await context.PostAsync($"How old is {person}? {JsonConvert.SerializeObject(result)}");
        }
        else if (!HasConversationData(context, InterestsEntityKey))
        {
            await context.PostAsync($"What are {person}s interests? {JsonConvert.SerializeObject(result)}");
        }
        else
        {
            await context.PostAsync($"Now I know everything");
            //Make recommendations
        }
        context.Wait(MessageReceived);
    }

    private bool HasConversationData(IDialogContext context, string key)
    {
        string value;
        if (context.ConversationData.TryGetValue(key, out value))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool TryGetConversationData(IDialogContext context, string key, out string value)
    {
        value = null;
        if (context.ConversationData.TryGetValue(key, out value))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool TryGetEntityData(LuisResult result, string key, out string value)
    {
        value = null;
       
        var entity = result.Entities.FirstOrDefault(x => x.Type.Equals(key, StringComparison.InvariantCultureIgnoreCase) && x.Score > 0.4);
        if (entity == null) return false;
        value = entity.Entity;
        return true;
    }

    private void FetchInspirationData(IDialogContext context, LuisResult result)
    {
        context.PostAsync($"looking for {JsonConvert.SerializeObject(result)}");
        var entities = new string[] { PersonEntityKey, AgeEntityKey, GenderEntityKey };
        foreach (var entityKey in entities)
        {
            context.PostAsync($"looking for {entityKey}");
            string data = "";
            if (TryGetEntityData(result, entityKey, out data))
            {
                context.PostAsync($"saving {data}");
                context.ConversationData.SetValue(entityKey, data);
            }
        }
        string person = "";
        if (TryGetEntityData(result, PersonEntityKey, out person))
        {
            person = person.ToLower();
            if (FemaleIdentifiers.Contains(person))
            {
                context.ConversationData.SetValue(GenderEntityKey, GenderFemale);
            }
            else if (MaleIdentifiers.Contains(person))
            {
                context.ConversationData.SetValue(GenderEntityKey, GenderMale);
            }
        }
    }




    // Go to https://luis.ai and create a new intent, then train/publish your luis app.
    // Finally replace "MyIntent" with the name of your newly created intent in the following handler
    [LuisIntent("Return")]
    public async Task Return(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Return intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("Clear")]
    public async Task Clear(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Clear intent"); //
        context.ConversationData.Clear();
        context.Wait(MessageReceived);
    }





}