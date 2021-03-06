﻿using System;
using System.Text.RegularExpressions;
using StorEvil.Core;
using StorEvil.Events;

namespace StorEvil.TeamCity
{
    public class TeamCityResultListener :
        IHandle<StoryStarting>,
        IHandle<ScenarioStarting>,
        IHandle<ScenarioFailed>,
        IHandle<ScenarioPassed>,
        IHandle<ScenarioPending>,
        IHandle<StoryFinished>,
        IHandle<LineFailed>, 
        IHandle<LinePending> 
    {
        private ITeamCityMessageWriter messageWriter;
        private const string TeamCityMessagePattern = "##teamcity[{0}]";

        private const string TestSuiteStartedMessagePattern = "testSuiteStarted name='{0}'";
        private const string TestSuiteFinishedMessagePattern = "testSuiteFinished name='{0}'";

        private const string TestStartedMessagePattern = "testStarted name='{0}'";
        private const string TestFailedMessagePattern = "testFailed name='{0}' message='{1}' details='{1}'";
        private const string TestFinishedMessagePattern = "testFinished name='{0}'";
        private const string TestIgnoredMessagePattern = "testIgnored name='{0}' message='{1}'";

        public TeamCityResultListener():this(new TeamCityMessageWriter())
        {        
        }

        public TeamCityResultListener(ITeamCityMessageWriter messageWriter)
        {
            this.messageWriter = messageWriter;
        }

        public void Handle(StoryStarting eventToHandle)
        {
            var summary = ExtractSummaryText(eventToHandle.Story);
            var message = Format(TestSuiteStartedMessagePattern, summary);
            SendTeamCityServiceMessage(message);
        }

        public void Handle(ScenarioStarting eventToHandle)
        {
            var message = Format(TestStartedMessagePattern, eventToHandle.Scenario.Name);
            SendTeamCityServiceMessage(message);
        }

        public void Handle(ScenarioFailed e)
        {
            SendScenarioFinishedMessage(e.Scenario.Name);
        }

        public void Handle(ScenarioPassed e)
        {
            SendScenarioFinishedMessage(e.Scenario.Name);
        }

        public void Handle(ScenarioPending e)
        {
            SendScenarioFinishedMessage(e.Scenario.Name);
        }

        private void SendScenarioFinishedMessage(string scenarioName)
        {
            var message = Format(TestFinishedMessagePattern, scenarioName);
            SendTeamCityServiceMessage(message);
        }

        public void Handle(StoryFinished eventToHandle)
        {
            var summary = ExtractSummaryText(eventToHandle.Story);
            var message = Format(TestSuiteFinishedMessagePattern, summary);
            SendTeamCityServiceMessage(message);
        }

        /// <summary>
        /// Implemented according to http://confluence.jetbrains.net/display/TCD5/Build+Script+Interaction+with+TeamCity
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string Encode(string message)
        {
            if (message == null) return null;

            message = message.Replace("|", "||")
                             .Replace("'", "|'")
                             .Replace("\n", "|n")
                             .Replace("\r", "|r")
                             .Replace("]", "|]")
                             .Replace("[", "|[");
            return message;
        }

        private string Format(string pattern, params string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Encode(values[i]);
            }
            return String.Format(pattern, values);
        }

        private void SendTeamCityServiceMessage(string message)
        {
            var teamCityMessage = String.Format(TeamCityMessagePattern, message);
            messageWriter.Write(teamCityMessage);
        }

        private string ExtractSummaryText(Story story)
        {
            var summary = Regex.Replace(story.Summary, @"feature\s*:\s*", String.Empty, RegexOptions.IgnoreCase);
            return StripNewlines(summary);
        }

        private string StripNewlines(string summary)
        {
            summary = summary.Replace("\r\n", " ")
                             .Replace("\n", " ");

            return Regex.Replace(summary, @"\s+", " ")
                        .Trim();
        }

        public void Handle(LineFailed e)
        {
            var message = Format(TestFailedMessagePattern, e.Scenario.Name, e.ExceptionInfo);
            SendTeamCityServiceMessage(message);
        }

        public void Handle(LinePending e)
        {
            var message = Format(TestIgnoredMessagePattern, e.Scenario.Name, e.Suggestion);
            SendTeamCityServiceMessage(message);
        }
    }
}
