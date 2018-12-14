using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;

namespace NicoPlayerHohoema.Models.Subscription
{

    public sealed class Subscription : BindableBase, IDisposable
    {
        public Guid Id { get; }

        private string _Label;
        public string Label
        {
            get { return _Label; }
            set { SetProperty(ref _Label, value); }
        }


        private bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { SetProperty(ref _IsEnabled, value); }
        }


        public ObservableCollection<SubscriptionSource> Sources { get; } = new ObservableCollection<SubscriptionSource>();

        public ObservableCollection<SubscriptionDestination> Destinations { get; } = new ObservableCollection<SubscriptionDestination>();


        private string _DoNotNoticeKeyword = string.Empty;
        public string DoNotNoticeKeyword
        {
            get { return _DoNotNoticeKeyword; }
            set { SetProperty(ref _DoNotNoticeKeyword, value); }
        }



        private bool _DoNotNoticeKeywordAsRegex = false;
        public bool DoNotNoticeKeywordAsRegex
        {
            get { return _DoNotNoticeKeywordAsRegex; }
            set { SetProperty(ref _DoNotNoticeKeywordAsRegex, value); }
        }

        private Regex _doNotNoticeKeywordRegex;
        private Regex DoNotNoticeeKeywordRegex
        {
            get
            {
                return _doNotNoticeKeywordRegex
                    ?? (_doNotNoticeKeywordRegex = new Regex(DoNotNoticeKeyword));
            }
        }

        private DelegateCommand<string> _UpdateDoNotNoticeKeyword;
        public DelegateCommand<string> UpdateDoNotNoticeKeyword
        {
            get
            {
                return _UpdateDoNotNoticeKeyword
                    ?? (_UpdateDoNotNoticeKeyword = new DelegateCommand<string>(doNotNoticeKeyword =>
                    {
                        DoNotNoticeKeyword = doNotNoticeKeyword;
                    },
                    doNotNoticeKeyword =>
                    {
                        if (DoNotNoticeKeywordAsRegex)
                        {
                            if (string.IsNullOrWhiteSpace(doNotNoticeKeyword))
                            {
                                return false;
                            }

                            try
                            {
                                return DoNotNoticeeKeywordRegex != null;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return !string.IsNullOrWhiteSpace(doNotNoticeKeyword);
                        }
                    }
                    ));
            }
        }



        public bool IsContainDoNotNoticeKeyword(string title)
        {
            if (string.IsNullOrWhiteSpace(DoNotNoticeKeyword)) { return false; }

            if (DoNotNoticeKeywordAsRegex)
            {
                return DoNotNoticeeKeywordRegex.IsMatch(title);
            }
            else
            {
                return title.Contains(DoNotNoticeKeyword);
            }
        }



        private int _UpdateTargetCount;
        public int UpdateTargetCount
        {
            get { return _UpdateTargetCount; }
            internal set { SetProperty(ref _UpdateTargetCount, value); }
        }

        private int _UpdateCompletedCount;
        public int UpdateCompletedCount
        {
            get { return _UpdateCompletedCount; }
            internal set { SetProperty(ref _UpdateCompletedCount, value); }
        }


        private SubscriptionUpdateStatus _Status = SubscriptionUpdateStatus.Complete;
        public SubscriptionUpdateStatus Status
        {
            get { return _Status; }
            internal set { SetProperty(ref _Status, value); }
        }


        public bool IsDeleted { get; internal set; } = false;


        CompositeDisposable _disposables = new CompositeDisposable();

        // instantiate from only on SubscriptionManager.
        internal Subscription(Guid id, string label)
        {
            Id = id;
            Label = label;

            new[] {
                this.ObserveProperty(x => x.DoNotNoticeKeywordAsRegex).ToUnit(),
                this.ObserveProperty(x => x.DoNotNoticeKeyword).ToUnit(),
            }
            .Merge()
            .Subscribe(x => 
            {
                _doNotNoticeKeywordRegex = null;
                UpdateDoNotNoticeKeyword.RaiseCanExecuteChanged();
            });
            
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }


        private DelegateCommand<string> _Rename;
        public DelegateCommand<string> Rename
        {
            get
            {
                return _Rename
                    ?? (_Rename = new DelegateCommand<string>(rename => 
                    {
                        Label = rename;
                    }, 
                    rename => 
                    {
                        return !string.IsNullOrWhiteSpace(rename);
                    }
                    ));
            }
        }


        private DelegateCommand _Remove;
        public DelegateCommand Remove
        {
            get
            {
                return _Remove
                    ?? (_Remove = new DelegateCommand(() =>
                    {
                        SubscriptionManager.Instance.Subscriptions.Remove(this);
                    },
                    () =>
                    {
                        return !this.IsDeleted;
                    }
                    ));
            }
        }


        private DelegateCommand<SubscriptionSource?> _RemoveSource;
        public DelegateCommand<SubscriptionSource?> RemoveSource
        {
            get
            {
                return _RemoveSource
                    ?? (_RemoveSource = new DelegateCommand<SubscriptionSource?>((source) =>
                    {
                        this.Sources.Remove(source.Value);
                    },
                    (source) =>
                    {
                        return source != null;
                    }
                    ));
            }
        }


        private DelegateCommand<SubscriptionSource?> _AddSource;
        public DelegateCommand<SubscriptionSource?> AddSource
        {
            get
            {
                return _AddSource
                    ?? (_AddSource = new DelegateCommand<SubscriptionSource?>((source) =>
                    {
                        this.Sources.Add(source.Value);
                    },
                    (source) =>
                    {
                        return source != null && this.Sources.All(x => !x.Equals(source.Value));
                    }
                    ));
            }
        }


    }

}
