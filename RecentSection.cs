// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Overlays.Profile.Sections.Recent;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Sections
{
    public partial class RecentSection : ProfileSection
    {
        public override LocalisableString Title => UsersStrings.ShowExtraRecentActivityTitle;

        public override string Identifier => @"recent_activity";

        public RecentSection()
        {
            // Display general recent activity.
            var recentActivityContainer = new PaginatedRecentActivityContainer(User);

            // Add custom sections for osu!mania 4K and 7K activities.
            var mania4KActivityContainer = new PaginatedRecentActivityContainer(User)
            {
                Filter = activity => activity.Ruleset.ShortName == "mania" && activity.KeyCount == 4,
                HeaderText = @"osu!mania 4K Recent Activity"
            };

            var mania7KActivityContainer = new PaginatedRecentActivityContainer(User)
            {
                Filter = activity => activity.Ruleset.ShortName == "mania" && activity.KeyCount == 7,
                HeaderText = @"osu!mania 7K Recent Activity"
            };

            // Add all sections to the children.
            Children = new[]
            {
                recentActivityContainer,
                mania4KActivityContainer,
                mania7KActivityContainer
            };
        }
    }
}
