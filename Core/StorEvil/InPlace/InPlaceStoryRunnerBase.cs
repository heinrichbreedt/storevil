using System.Collections.Generic;
using System.Linq;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Parsing;

namespace StorEvil.InPlace
{
    public abstract class InPlaceStoryRunnerBase : IStoryHandler
    {
        protected readonly IResultListener ResultListener;
        private readonly IStoryFilter _filter;
        private readonly IScenarioPreprocessor _preprocessor;

        protected InPlaceStoryRunnerBase(IResultListener resultListener,
                                         IScenarioPreprocessor preprocessor,
                                         IStoryFilter filter)
        {
            ResultListener = resultListener;
            _preprocessor = preprocessor;
            _filter = filter;
        }

        public int HandleStory(Story story, StoryContext context)
        {
            ResultListener.StoryStarting(story);
            IEnumerable<Scenario> scenariosMatchingFilter = GetScenariosMatchingFilter(story);

            return Execute(story, scenariosMatchingFilter, context);
        }

        protected abstract int Execute(Story story, IEnumerable<Scenario> scenariosMatchingFilter, StoryContext context);

        private IEnumerable<Scenario> GetScenariosMatchingFilter(Story story)
        {
            return GetScenarios(story).Where(s => _filter.Include(story, s));
        }

        private IEnumerable<Scenario> GetScenarios(Story story)
        {
            return story.Scenarios.SelectMany(scenario => _preprocessor.Preprocess(scenario));
        }

        public void Finished()
        {
            ResultListener.Finished();
        }
    }
}