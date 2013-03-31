﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using MVCForum.Domain.DomainModel;
using MVCForum.Website.Application;

namespace MVCForum.Website.ViewModels
{
    public class ActiveTopicsViewModel
    {
        public PagedList<Topic> Topics { get; set; }
        public Dictionary<Category, PermissionSet> AllPermissionSets { get; set; }

        public int? PageIndex { get; set; }
        public int? TotalCount { get; set; }

        public MembershipUser User { get; set; }
    }

    public class TagTopicsViewModel
    {
        public PagedList<Topic> Topics { get; set; }
        public Dictionary<Category, PermissionSet> AllPermissionSets { get; set; }

        public int? PageIndex { get; set; }
        public int? TotalCount { get; set; }

        public string Tag { get; set; }

        public MembershipUser User { get; set; }
    }

    public class CreateTopicViewModel
    {
        [Required]
        [StringLength(600)]
        [ForumMvcResourceDisplayName("Topic.Label.TopicTitle")]
        public string Name { get; set; }

        //[UIHint("bbeditor"), AllowHtml]
        [UIHint("markdowneditor"), AllowHtml]
        //[UIHint("tinymceeditor")]
        public string Content { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }

        [Required]
        public Guid Category { get; set; }
        public string Tags { get; set; }

        public IEnumerable<Category> Categories { get; set; }

        public List<PollAnswer> PollAnswers { get; set; }
            
        [ForumMvcResourceDisplayName("Topic.Label.SubscribeToTopic")]
        public bool SubscribeToTopic { get; set; }
    }

    public class ShowTopicViewModel
    {
        public Topic Topic { get; set; }
        public PagedList<Post> Posts { get; set; }
        public PermissionSet Permissions { get; set; }
        public int? PageIndex { get; set; }
        public int? TotalCount { get; set; }
        public MembershipUser User { get; set; }
        public bool IsSubscribed { get; set; }
        public bool UserHasAlreadyVotedInPoll { get; set; }
        public int TotalVotesInPoll { get; set; }
    }

    public class ShowPollViewModel
    {
        public Poll Poll { get; set; }
        public bool UserHasAlreadyVoted { get; set; }
        public int TotalVotesInPoll { get; set; }
        public bool UserAllowedToVote { get; set; }
    }

    public class UpdatePollViewModel
    {
        public Guid PollId { get; set; }
        public Guid AnswerId { get; set; }
    }

    public class ViewTopicViewModel
    {
        public Topic Topic { get; set; }
        public PermissionSet Permissions { get; set; }
        public MembershipUser User { get; set; }
        public bool ShowCategoryName { get; set; }
    }

    public class MoveTopicViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public List<Category> Categories { get; set; }
    }

    public class NotifyNewTopicViewModel
    {
        public Guid CategoryId { get; set; }
    }
}