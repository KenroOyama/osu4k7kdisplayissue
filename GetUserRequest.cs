// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    public class GetUserRequest : APIRequest<APIUser>
    {
        public readonly string Lookup;
        public readonly IRulesetInfo? Ruleset;
        private readonly LookupType lookupType;

        /// <summary>
        /// Gets a user from their ID.
        /// </summary>
        /// <param name="userId">The user to get.</param>
        /// <param name="ruleset">The ruleset to get the user's info for.</param>
        public GetUserRequest(long? userId = null, IRulesetInfo? ruleset = null)
        {
            Lookup = userId.ToString()!;
            lookupType = LookupType.Id;
            Ruleset = ruleset;
        }

        /// <summary>
        /// Gets a user from their username.
        /// </summary>
        /// <param name="username">The user to get.</param>
        /// <param name="ruleset">The ruleset to get the user's info for.</param>
        public GetUserRequest(string username, IRulesetInfo? ruleset = null)
        {
            Lookup = username;
            lookupType = LookupType.Username;
            Ruleset = ruleset;
        }

        protected override string Target
        {
            get
            {
                string baseTarget = $@"users/{Lookup}/{Ruleset?.ShortName}?key={lookupType.ToString().ToLowerInvariant()}";
                
                // Add extra parameters for fetching osu!mania 4K and 7K stats
                if (Ruleset?.ShortName == "mania")
                {
                    baseTarget += "&include_4k_7k=true";
                }

                return baseTarget;
            }
        }

        private enum LookupType
        {
            Id,
            Username
        }
    }
}
