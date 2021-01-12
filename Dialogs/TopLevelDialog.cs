// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.BotBuilderSamples
{
    public class TopLevelDialog : ComponentDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "done";

        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        private System.Timers.Timer aTimer;

        static bool timerStarted = false;

        public TopLevelDialog()
            : base(nameof(TopLevelDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));

            AddDialog(new ReviewSelectionDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                //AgeStepAsync,
                StartSelectionStepAsync,
                AcknowledgementStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create an object in which to collect the user's information within the dialog.
            stepContext.Values[UserInfo] = new UserProfile();

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Please enter text.") };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);


        }

        //private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    // Set the user's name to what they entered in response to the name prompt.
        //    var userProfile = (UserProfile)stepContext.Values[UserInfo];
        //    userProfile.Name = (string)stepContext.Result;

        //    var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Please enter your age.") };

        //    // Ask the user to enter their age.
        //    return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        //}

        private async Task<DialogTurnResult> StartSelectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string userText = (string)stepContext.Result;

            if (userText.ToLower().Contains("stop")){
                StopPolling();
                await stepContext.Context.SendActivityAsync("timer was stopped");
                return await stepContext.EndDialogAsync();
            }

            if (!timerStarted)
            {//setup a timer
                StartPolling(stepContext);
            }
            //replace the dialog
            return await stepContext.ReplaceDialogAsync(InitialDialogId);

            //// Set the user's age to what they entered in response to the age prompt.
            //var userProfile = (UserProfile)stepContext.Values[UserInfo];
            //userProfile.Age = (int)stepContext.Result;

            //if (userProfile.Age < 25)
            //{
            //    // If they are too young, skip the review selection dialog, and pass an empty list to the next step.
            //    await stepContext.Context.SendActivityAsync(
            //        MessageFactory.Text("You must be 25 or older to participate."),
            //        cancellationToken);
            //    return await stepContext.NextAsync(new List<string>(), cancellationToken);
            //}
            //else
            //{
            //    // Otherwise, start the review selection dialog.
            //    return await stepContext.BeginDialogAsync(nameof(ReviewSelectionDialog), null, cancellationToken);
            //}
        }


        //for handling timers
        public void StartPolling(DialogContext dialogContext)
        {
            //Dispose any existing timers if any
            StopPolling();

            // Create a timer with a 7 second interval.
            aTimer = new System.Timers.Timer(7000);
            // Hook up the Elapsed event for the timer. 
            // aTimer.Elapsed += (source, e) => OnTimedEvent(source, e, dialogContext, request);
            aTimer.Elapsed += (source, e) => OnTimedEvent(source, e, dialogContext);
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public void StopPolling()
        {
            if (aTimer != null)
            {
                aTimer.Stop();
                aTimer.Close();
                aTimer.Dispose();
            }
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e, DialogContext dialogContext)
        {
        string formattedText = "this is from timer, type stop to stop the timer";
        await dialogContext.Context.SendActivityAsync(MessageFactory.Text(formattedText, formattedText));
        }


        private async Task<DialogTurnResult> AcknowledgementStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's company selection to what they entered in the review-selection dialog.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.CompaniesToReview = stepContext.Result as List<string> ?? new List<string>();

            // Thank them for participating.
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Thanks for participating, {((UserProfile)stepContext.Values[UserInfo]).Name}."),
                cancellationToken);

            // Exit the dialog, returning the collected user information.
            return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        }
    }
}
