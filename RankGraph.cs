// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Users;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class RankGraph : UserGraph<int, int>
    {
        private const int ranked_days = 88;

        public readonly Bindable<UserStatistics?> Statistics = new Bindable<UserStatistics?>();

        private readonly OsuSpriteText placeholder;

        public RankGraph()
        {
            Add(placeholder = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = UsersStrings.ShowExtraUnranked,
                Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular)
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Statistics.BindValueChanged(statistics => updateStatistics(statistics.NewValue), true);
        }

        private void updateStatistics(UserStatistics? statistics)
        {
            if (statistics == null)
            {
                Data = null;
                return;
            }

            // Fetch global rank history
            int[]? userRanks = statistics.IsRanked == true ? statistics.RankHistory?.Data : null;

            // Fetch 4K rank history
            int[]? mania4KRanks = statistics.IsRanked == true ? statistics.Mania4KRankHistory?.Data : null;

            // Fetch 7K rank history
            int[]? mania7KRanks = statistics.IsRanked == true ? statistics.Mania7KRankHistory?.Data : null;

            // Combine data or show individual graphs based on implementation
            Data = userRanks?.Select((x, index) => new KeyValuePair<int, int>(index, x))
                            .Where(x => x.Value != 0)
                            .ToArray();

            if (mania4KRanks != null)
            {
                // Logic to handle 4K rank data visualization
                // Example: Add to separate Data container or overlay graph logic
            }

            if (mania7KRanks != null)
            {
                // Logic to handle 7K rank data visualization
                // Example: Add to separate Data container or overlay graph logic
            }
        }

        protected override float GetDataPointHeight(int rank) => -MathF.Log(rank);

        protected override void ShowGraph()
        {
            base.ShowGraph();
            placeholder.FadeOut(FADE_DURATION, Easing.Out);
        }

        protected override void HideGraph()
        {
            base.HideGraph();
            placeholder.FadeIn(FADE_DURATION, Easing.Out);
        }

        protected override UserGraphTooltipContent GetTooltipContent(int index, int rank)
        {
            int days = ranked_days - index + 1;

            return new UserGraphTooltipContent
            {
                Name = UsersStrings.ShowRankGlobalSimple,
                Count = rank.ToLocalisableString("\\##,##0"),
                Time = days == 0 ? "now" : $"{"day".ToQuantity(days)} ago",
            };
        }
    }
}
