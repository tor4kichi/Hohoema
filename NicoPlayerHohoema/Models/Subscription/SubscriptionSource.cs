namespace NicoPlayerHohoema.Models.Subscription
{
    public struct SubscriptionSource 
    {
        public string Label { get; set; }
        public SubscriptionSourceType SourceType { get; }
        public string Parameter { get; }

        public SubscriptionSource(string label, SubscriptionSourceType sourceType, string parameter)
        {
            _HashCode = null;

            Label = label;
            SourceType = sourceType;
            Parameter = parameter;

        }

        public override bool Equals(object obj)
        {
            if (obj is SubscriptionSource other)
            {
                return this.SourceType == other.SourceType
                    && this.Parameter == other.Parameter;
            }

            return base.Equals(obj);
        }

        int? _HashCode;
        public override int GetHashCode()
        {
            return _HashCode ?? (_HashCode = (Parameter + SourceType.ToString()).GetHashCode()).Value;
        }
    }

}
