namespace RetroCoreFit
{
    public class RetroClient
    {
    }

    public struct RestParameter {

        public ParameterType Type { get; set; }

        public string Name { get; set; }

        public object Value { get; set; }

    }

    public enum ParameterType {
        Body,
        Query,
        Header,
        Path,
        Cookie
    }


}
