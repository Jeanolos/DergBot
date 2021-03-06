﻿using Newtonsoft.Json;
using System;

namespace OakBot.Models
{
    [JsonObject("streams")]
    public class Stream
    {
        #region Public Properties

        [JsonProperty("average_fps")]
        public double AverageFps { get; set; }

        [JsonProperty("channel")]
        public Channel Channel { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("delay")]
        public long Delay { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("_id")]
        public long Id { get; set; }

        [JsonProperty("preview")]
        public ScaledImage Preview { get; set; }

        [JsonProperty("video_height")]
        public double VideoHeight { get; set; }

        [JsonProperty("viewers")]
        public long Viewers { get; set; }

        #endregion Public Properties
    }
}