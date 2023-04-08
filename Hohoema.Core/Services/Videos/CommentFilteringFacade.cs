using Hohoema.Models;
using Hohoema.Models.Player;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Application;
using Hohoema.Models.Player.Comment;
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Contracts.Services;

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
    private readonly AppFlagsRepository _appFlagsRepository;

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

    void Initialize()
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

    public void AddCenterCommandFiltering()
    {
        AddFilteredCommentCommand("naka");
        AddFilteredCommentCommand("center");
    }



    #region ICommentFilter

    public bool IsHiddenComment(IComment comment)
    {
        if (IsHiddenShareNGScore(comment.NGScore))
        {
            return true;
        }

        if (IsEnableFilteringCommentOwnerId
            && IsHiddenCommentOwnerUserId(comment.UserId)
            )
        {
            return true;
        }

        if (IsEnableFilteringCommentText 
            && _filteringCommentTextKeywords.IsMatchAny(comment.CommentText)
            )
        {
            return true;
        }

        return false;
    }


    public string TransformCommentText(string commentText)
    {
        return TranformCommentText(commentText);
    }

    HashSet<string> _ignoreCommands;

    public bool IsIgnoreCommand(string command)
    {
        return _ignoreCommands.Contains(command);
    }

    #endregion

    RelayCommand<string> _AddFilteredCommentCommandCommand;
    public RelayCommand<string> AddFilteredCommentCommandCommand => _AddFilteredCommentCommandCommand
        ?? (_AddFilteredCommentCommandCommand = new RelayCommand<string>((commandText) =>
        {
            AddFilteredCommentCommand(commandText);
        }));

    public void AddFilteredCommentCommand(string commandText)
    {
        if (_ignoreCommands.Contains(commandText))
        {
            return;
        }

        _ignoreCommands.Add(commandText);
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


    List<CommentFliteringRepository.CommentTextTransformCondition> _commentTextTransformConditions;

    public string TranformCommentText(string commentText)
    {
        return _commentTextTransformConditions.TransformCommentText(commentText);
    }

    RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _AddTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> AddTextTransformConditionsCommand => _AddTextTransformConditionsCommand
        ?? (_AddTextTransformConditionsCommand = new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
        {
            AddTextTransformConditions(condition ?? new CommentFliteringRepository.CommentTextTransformCondition());
        }));

    public void AddTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        var added = _commentFliteringRepository.AddCommentTextTransformCondition(condition.RegexPattern, condition.ReplaceText, condition.Description);
        _commentTextTransformConditions.Add(added);
        TransformConditionAdded?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = added });
    }

    RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _UpdateTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> UpdateTextTransformConditionsCommand => _UpdateTextTransformConditionsCommand
        ?? (_UpdateTextTransformConditionsCommand = new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
        {
            UpdateTextTransformConditions(condition);
        }));

    public void UpdateTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        if (condition == null) { return; }

        _commentFliteringRepository.UpdateCommentTextTransformCondition(condition);
        TransformConditionUpdated?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = condition });
    }


    RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> _RemoveTextTransformConditionsCommand;
    public RelayCommand<CommentFliteringRepository.CommentTextTransformCondition> RemoveTextTransformConditionsCommand => _RemoveTextTransformConditionsCommand
        ?? (_RemoveTextTransformConditionsCommand = new RelayCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
        {
            RemoveTextTransformConditions(condition);
        }));

    public void RemoveTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
    {
        if (_commentFliteringRepository.RemoveCommentTextTransformCondition(condition))
        {
            _commentTextTransformConditions.Remove(condition);
            TransformConditionRemoved?.Invoke(this, new CommentTextTranformConditionChangedArgs() { TransformCondition = condition });
        }
    }

    #endregion


    #region Share NG Score 

    public bool IsHiddenShareNGScore(int score)
    {
        return _commentFliteringRepository.ShareNGScore >= score;
    }

    private int _shareNGScore;
    public int ShareNGScore
    {
        get { return _shareNGScore; }
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

    HashSet<string> _filteredCommentOwnerIds = new HashSet<string>();

    public bool IsHiddenCommentOwnerUserId(string userId)
    {
        if (!_commentFliteringRepository.IsFilteringCommentOwnerIdEnabled) { return false; }

        return _filteredCommentOwnerIds.Contains(userId);
    }

    private bool _IsEnableFilteringCommentOwnerId;
    public bool IsEnableFilteringCommentOwnerId
    {
        get => _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled;
        set
        {
            _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled = value;
            SetProperty(ref _IsEnableFilteringCommentOwnerId, value);
        }
    }


   public List<CommentFliteringRepository.FilteringCommentOwnerId> GetFilteringCommentOwnerIdList()
    {
        return _commentFliteringRepository.GetAllFilteringCommenOwnerId();
    }

    RelayCommand<IComment> _AddFilteringCommentOwnerIdCommand;
    public RelayCommand<IComment> AddFilteringCommentOwnerIdCommand => _AddFilteringCommentOwnerIdCommand
        ?? (_AddFilteringCommentOwnerIdCommand = new RelayCommand<IComment>((comment) => 
        {
            AddFilteringCommentOwnerId(comment.UserId, comment.CommentText);
        }));

    RelayCommand<IComment> _RemoveFilteringCommentOwnerIdCommand;
    public RelayCommand<IComment> RemoveFilteringCommentOwnerIdCommand => _RemoveFilteringCommentOwnerIdCommand
        ?? (_RemoveFilteringCommentOwnerIdCommand = new RelayCommand<IComment>((comment) =>
        {
            RemoveFilteringCommentOwnerId(comment.UserId);
        }));

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
            var result = _commentFliteringRepository.RemoveFilteringCommenOwnerId(userId);

            FilteringCommentOwnerIdRemoved?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = userId });

            return true;
        }
        else { return false; }
    }
    private RelayCommand _ClearFilteringCommentUserIdCommand;
    public RelayCommand ClearFilteringCommentUserIdCommand => _ClearFilteringCommentUserIdCommand
        ?? (_ClearFilteringCommentUserIdCommand = new RelayCommand(() =>
        {
            foreach (var id in _filteredCommentOwnerIds.ToArray())
            {
                _filteredCommentOwnerIds.Remove(id);

                _commentFliteringRepository.RemoveFilteringCommenOwnerId(id);

                FilteringCommentOwnerIdRemoved?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = id });
            }
        }));

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
    public bool IsEnableFilteringCommentText
    {
        get => _commentFliteringRepository.IsFilteringCommentTextEnabled;
        set
        {
            _commentFliteringRepository.IsFilteringCommentTextEnabled = value;
            SetProperty(ref _IsEnableFilteringCommentText, value);
        }
    }


    ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword> _filteringCommentTextKeywords;
    public IEnumerable<CommentFliteringRepository.FilteringCommentTextKeyword> GetAllFilteringCommentTextCondition()
    {
        return _filteringCommentTextKeywords;
    }



    RelayCommand<string> _AddFilteringCommentTextConditionCommand;
    public RelayCommand<string> AddFilteringCommentTextConditionCommand => _AddFilteringCommentTextConditionCommand
        ?? (_AddFilteringCommentTextConditionCommand = new RelayCommand<string>((keyword) =>
        {
            AddFilteringCommentTextKeyword(keyword);
        }));

    public void AddFilteringCommentTextKeyword(string keyword)
    {
        var added =_commentFliteringRepository.AddFilteringCommentText(keyword);
        _filteringCommentTextKeywords.Insert(0, added);
        FilterKeywordAdded?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = added });
    }

    RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _UpdateFilteringCommentTextConditionCommand;
    public RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> UpdateFilteringCommentTextConditionCommand => _UpdateFilteringCommentTextConditionCommand
        ?? (_UpdateFilteringCommentTextConditionCommand = new RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword>((keyword) =>
        {
            UpdateFilteringCommentTextKeyword(keyword);
        }));

    public void UpdateFilteringCommentTextKeyword(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
    {
        _commentFliteringRepository.UpdateFilteringCommentText(keyword);
        
        FilterKeywordUpdated?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = keyword });
    }


    RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _RemoveFilteringCommentTextConditionCommand;
    public RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword> RemoveFilteringCommentTextConditionCommand => _RemoveFilteringCommentTextConditionCommand
        ?? (_RemoveFilteringCommentTextConditionCommand = new RelayCommand<CommentFliteringRepository.FilteringCommentTextKeyword>((keyword) =>
        {
            RemoveFilteringCommentTextCondition(keyword);
        }));

    public void RemoveFilteringCommentTextCondition(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
    {
        if (_commentFliteringRepository.RemoveFilteringCommentText(keyword))
        {
            _filteringCommentTextKeywords.Remove(keyword);
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
