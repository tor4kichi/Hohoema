#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Contracts.Services;
using Hohoema.Models.Application;
using Hohoema.Models.Player.Comment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hohoema.Services.Player.Videos;

public sealed class CommentFilteringFacade : ObservableObject, ICommentFilter
{
    public class CommentOwnerIdFilteredEventArgs
    {
        public string UserId { get; set; }
    }


    public event EventHandler<CommentOwnerIdFilteredEventArgs> FilteringCommentOwnerIdAdded;
    public event EventHandler<CommentOwnerIdFilteredEventArgs> FilteringCommentOwnerIdRemoved;

    private readonly ILocalizeService _localizeService;
    private readonly CommentFliteringRepository _commentFliteringRepository;
    [Obsolete]
    private readonly AppFlagsRepository _appFlagsRepository;

    [Obsolete]
    public CommentFilteringFacade(
        ILocalizeService localizeService,
        CommentFliteringRepository commentFliteringRepository,
        AppFlagsRepository appFlagsRepository
        )
    {
        _localizeService = localizeService;
        _commentFliteringRepository = commentFliteringRepository;
        _appFlagsRepository = appFlagsRepository;
        _commentTextTransformConditions = _commentFliteringRepository.GetAllCommentTextTransformCondition();
        _filteredCommentOwnerIds = _commentFliteringRepository.GetAllFilteringCommenOwnerId().Select(x => x.UserId).ToHashSet();
        _filteringCommentTextKeywords = new ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword>(
            _commentFliteringRepository.GetAllFilteringCommentTextConditions()
            );
        _ignoreCommands = _commentFliteringRepository.GetFilteredCommands().ToHashSet();

        _shareNGScore = _commentFliteringRepository.ShareNGScore;

        Initialize();
    }

    [Obsolete]
    private void Initialize()
    {
        if (!_appFlagsRepository.IsInitializedCommentFilteringCondition)
        {
            AddGlassMowerCommentTextTransformCondition();
            AddCenterCommandFiltering();
            _appFlagsRepository.IsInitializedCommentFilteringCondition = true;
        }
    }

    public void AddGlassMowerCommentTextTransformCondition()
    {
        AddTextTransformConditions(new CommentFliteringRepository.CommentTextTransformCondition()
        {
            RegexPattern = "[wWｗＷ]{2,}",
            ReplaceText = "ｗ",
            Description = _localizeService.Translate("AutoShortingKUSAWords"),
        });
    }

    [Obsolete]
    public void AddCenterCommandFiltering()
    {
        AddFilteredCommentCommand("naka");
        AddFilteredCommentCommand("center");
    }



    #region ICommentFilter

    [Obsolete]
    public bool IsHiddenComment(IComment comment)
    {
        if (IsHiddenShareNGScore(comment.NGScore))
        {
            return true;
        }

        return IsEnableFilteringCommentOwnerId
            && IsHiddenCommentOwnerUserId(comment.UserId)
|| IsEnableFilteringCommentText
            && _filteringCommentTextKeywords.IsMatchAny(comment.CommentText);
    }


    public string TransformCommentText(string commentText)
    {
        return TranformCommentText(commentText);
    }

    private readonly HashSet<string> _ignoreCommands;

    public bool IsIgnoreCommand(string command)
    {
        return _ignoreCommands.Contains(command);
    }

    #endregion

    private RelayCommand<string> _AddFilteredCommentCommandCommand;

    [Obsolete]
    public RelayCommand<string> AddFilteredCommentCommandCommand => _AddFilteredCommentCommandCommand ??= new RelayCommand<string>(AddFilteredCommentCommand);

    [Obsolete]
    public void AddFilteredCommentCommand(string commandText)
    {
        if (_ignoreCommands.Contains(commandText))
        {
            return;
        }

        _ = _ignoreCommands.Add(commandText);
        _commentFliteringRepository.AddFilteredCommand(commandText);
    }


    #region Comment Text Transform

    public class CommentTextTranformConditionChangedArgs
    {
        public CommentFliteringRepository.CommentTextTransformCondition TransformCondition { get; set; }
    }

    public event EventHandler<CommentTextTranformConditionChangedArgs> TransformConditionAdded;
    public event EventHandler<CommentTextTranformConditionChangedArgs> TransformConditionUpdated;
    public event EventHandler<CommentTextTranformConditionChangedArgs> TransformConditionRemoved;


    public IEnumerable<CommentFliteringRepository.CommentTextTransformCondition> GetTextTranformConditions()
    {
        return _commentTextTransformConditions.ToList();
    }

    private readonly List<CommentFliteringRepository.CommentTextTransformCondition> _commentTextTransformConditions;

    public string TranformCommentText(string commentText)
    {
        return _commentTextTransformConditions.TransformCommentText(commentText);
    }

    private RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _AddTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> AddTextTransformConditionsCommand => _AddTextTransformConditionsCommand ??= new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
        {
            AddTextTransformConditions(condition ?? new CommentFliteringRepository.CommentTextTransformCondition());
        });

    public void AddTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        CommentFliteringRepository.CommentTextTransformCondition added = _commentFliteringRepository.AddCommentTextTransformCondition(condition.RegexPattern, condition.ReplaceText, condition.Description);
        _commentTextTransformConditions.Add(added);
        TransformConditionAdded?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = added });
    }

    private RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _UpdateTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> UpdateTextTransformConditionsCommand => _UpdateTextTransformConditionsCommand ??= new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>(UpdateTextTransformConditions);

    public void UpdateTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        if (condition == null) { return; }

        _commentFliteringRepository.UpdateCommentTextTransformCondition(condition);
        TransformConditionUpdated?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = condition });
    }

    private RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _RemoveTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> RemoveTextTransformConditionsCommand => _RemoveTextTransformConditionsCommand ??= new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>(RemoveTextTransformConditions);

    public void RemoveTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        if (_commentFliteringRepository.RemoveCommentTextTransformCondition(condition))
        {
            _ = _commentTextTransformConditions.Remove(condition);
            TransformConditionRemoved?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = condition });
        }
    }

    #endregion


    #region Share NG Score 

    [Obsolete]
    public bool IsHiddenShareNGScore(int score)
    {
        return _commentFliteringRepository.ShareNGScore >= score;
    }

    private int _shareNGScore;

    [Obsolete]
    public int ShareNGScore
    {
        get => _shareNGScore;
        set
        {
            if (SetProperty(ref _shareNGScore, value))
            {
                _commentFliteringRepository.ShareNGScore = value;
            }
        }
    }

    #endregion


    #region Filtered Comment Owner Id

    private readonly HashSet<string> _filteredCommentOwnerIds = new();

    [Obsolete]
    public bool IsHiddenCommentOwnerUserId(string userId)
    {
        return _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled && _filteredCommentOwnerIds.Contains(userId);
    }

    private bool _IsEnableFilteringCommentOwnerId;

    [Obsolete]
    public bool IsEnableFilteringCommentOwnerId
    {
        get => _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled;
        set
        {
            _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled = value;
            _ = SetProperty(ref _IsEnableFilteringCommentOwnerId, value);
        }
    }


    public List<CommentFliteringRepository.FilteringCommentOwnerId> GetFilteringCommentOwnerIdList()
    {
        return _commentFliteringRepository.GetAllFilteringCommenOwnerId();
    }

    private RelayCommand<IComment> _AddFilteringCommentOwnerIdCommand;
    public RelayCommand<IComment> AddFilteringCommentOwnerIdCommand => _AddFilteringCommentOwnerIdCommand ??= new RelayCommand<IComment>((comment) =>
        {
            _ = AddFilteringCommentOwnerId(comment.UserId, comment.CommentText);
        });

    private RelayCommand<IComment> _RemoveFilteringCommentOwnerIdCommand;
    public RelayCommand<IComment> RemoveFilteringCommentOwnerIdCommand => _RemoveFilteringCommentOwnerIdCommand ??= new RelayCommand<IComment>((comment) =>
        {
            _ = RemoveFilteringCommentOwnerId(comment.UserId);
        });

    public bool AddFilteringCommentOwnerId(string userId, string commentText)
    {
        if (string.IsNullOrEmpty(userId)) { return false; }

        if (_filteredCommentOwnerIds.Add(userId))
        {
            _commentFliteringRepository.AddFilteringCommenOwnerId(userId, commentText);

            FilteringCommentOwnerIdAdded?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = userId });

            return true;
        }
        else { return false; }
    }

    public bool RemoveFilteringCommentOwnerId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) { return false; }

        if (_filteredCommentOwnerIds.Remove(userId))
        {
            bool result = _commentFliteringRepository.RemoveFilteringCommenOwnerId(userId);

            FilteringCommentOwnerIdRemoved?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = userId });

            return true;
        }
        else { return false; }
    }
    private RelayCommand _ClearFilteringCommentUserIdCommand;
    public RelayCommand ClearFilteringCommentUserIdCommand => _ClearFilteringCommentUserIdCommand ??= new RelayCommand(() =>
        {
            foreach (string id in _filteredCommentOwnerIds.ToArray())
            {
                _ = _filteredCommentOwnerIds.Remove(id);

                _ = _commentFliteringRepository.RemoveFilteringCommenOwnerId(id);

                FilteringCommentOwnerIdRemoved?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = id });
            }
        });

    #endregion


    #region Filtering Comment Text

    public sealed class FilteringCommentTextKeywordEventArgs
    {
        public CommentFliteringRepository.FilteringCommentTextKeyword FilterKeyword { get; set; }
    }


    public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordAdded;
    public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordUpdated;
    public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordRemoved;



    private bool _IsEnableFilteringCommentText;

    [Obsolete]
    public bool IsEnableFilteringCommentText
    {
        get => _commentFliteringRepository.IsFilteringCommentTextEnabled;
        set
        {
            _commentFliteringRepository.IsFilteringCommentTextEnabled = value;
            _ = SetProperty(ref _IsEnableFilteringCommentText, value);
        }
    }

    private readonly ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword> _filteringCommentTextKeywords;
    public IEnumerable<CommentFliteringRepository.FilteringCommentTextKeyword> GetAllFilteringCommentTextCondition()
    {
        return _filteringCommentTextKeywords;
    }

    private RelayCommand<string> _AddFilteringCommentTextConditionCommand;
    public RelayCommand<string> AddFilteringCommentTextConditionCommand => _AddFilteringCommentTextConditionCommand ??= new RelayCommand<string>(AddFilteringCommentTextKeyword);

    public void AddFilteringCommentTextKeyword(string keyword)
    {
        CommentFliteringRepository.FilteringCommentTextKeyword added = _commentFliteringRepository.AddFilteringCommentText(keyword);
        _filteringCommentTextKeywords.Insert(0, added);
        FilterKeywordAdded?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = added });
    }

    private RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _UpdateFilteringCommentTextConditionCommand;
    public RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> UpdateFilteringCommentTextConditionCommand => _UpdateFilteringCommentTextConditionCommand ??= new RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword>(UpdateFilteringCommentTextKeyword);

    public void UpdateFilteringCommentTextKeyword(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
    {
        _commentFliteringRepository.UpdateFilteringCommentText(keyword);

        FilterKeywordUpdated?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = keyword });
    }

    private RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _RemoveFilteringCommentTextConditionCommand;
    public RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> RemoveFilteringCommentTextConditionCommand => _RemoveFilteringCommentTextConditionCommand ??= new RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword>(RemoveFilteringCommentTextCondition);

    public void RemoveFilteringCommentTextCondition(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
    {
        if (_commentFliteringRepository.RemoveFilteringCommentText(keyword))
        {
            _ = _filteringCommentTextKeywords.Remove(keyword);
            FilterKeywordRemoved?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = keyword });
        }
    }

    #endregion
}


public static class CommentTextTransformConditionExtensions
{
    public static string TransformCommentText(this IEnumerable<CommentFliteringRepository.CommentTextTransformCondition> conditions, string commentText)
    {
        return conditions.Aggregate(commentText, (text, condition) => condition.IsEnabled ? condition.TextTransform(text) : text);
    }
}

public static class FilteringCommentTextKeywordExtensions
{
    public static bool IsMatchAny(this IEnumerable<CommentFliteringRepository.FilteringCommentTextKeyword> keywords, string commentText)
    {
        return keywords.Any(x => x.IsMatch(commentText));
    }
}
