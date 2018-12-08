
namespace NicoPlayerHohoema.Models.Subscription
{
    public struct SubscriptionSource 
    {
        public SubscriptionSourceType SourceType { get; }
        public string Parameter { get; }

        public string Label { get; }

        public string OptionalLabel { get; }

        public SubscriptionSource(string label, SubscriptionSourceType sourceType, string parameter, string optionalLabel = null)
        {
            _HashCode = null;

            Label = label;
            SourceType = sourceType;
            Parameter = parameter;
            OptionalLabel = optionalLabel;
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
