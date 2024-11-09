using Microsoft.Extensions.Options;
using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.YouTube.Services;

internal sealed class PlaylistRuleResolver : IPlaylistRuleResolver
{
    private readonly PlaylistId _defaultPlaylistId;
    private readonly PlaylistRule[] _rules;

    public PlaylistRuleResolver(IOptions<YouTubeOptions> youTubeOptions)
    {
        var options = youTubeOptions.Value;
        _defaultPlaylistId = new PlaylistId(options.PlaylistId);
        _rules = options.PlaylistRules.Select(rule => new PlaylistRule(rule)).ToArray();
    }

    public PlaylistId Resolve(Video video)
    {
        return _rules
            .Where(rule => rule.IsMatchingCriteria(video))
            .Select(rule => rule.PlaylistId)
            .FirstOrDefault() ?? _defaultPlaylistId;
    }

    private record PlaylistRule
    {
        private const StringSplitOptions SplitOptions =
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        private readonly HashSet<string> _channelNames;
        private readonly HashSet<string> _titleKeywords;

        public PlaylistId PlaylistId { get; }

        public PlaylistRule(PlaylistRuleOptions playlistRuleOptions)
        {
            if (string.IsNullOrWhiteSpace(playlistRuleOptions.PlaylistId))
                throw new ApplicationException("Missing playlist id value of playlist rule");

            PlaylistId = new PlaylistId(playlistRuleOptions.PlaylistId);

            _channelNames = playlistRuleOptions.Channels.Split(',', SplitOptions)
                .Select(channel => channel.ToLowerInvariant())
                .ToHashSet();

            _titleKeywords = playlistRuleOptions.TitleKeywords.Split(',', SplitOptions).ToHashSet();
        }

        public bool IsMatchingCriteria(Video video)
        {
            bool? match = null;

            if (match is null or true && _channelNames.Count > 0)
            {
                match = _channelNames.Contains(video.ChannelTitle.ToLowerInvariant());
            }

            if (match is null or true && _titleKeywords.Count > 0)
            {
                match = _titleKeywords.Any(keyword => video.Title.Contains(keyword));
            }

            return match.HasValue && match.Value;
        }
    }
}
