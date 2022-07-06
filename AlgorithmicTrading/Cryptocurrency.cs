public enum State { Default, Buy, Sell, Hold };

struct Cryptocurrency {
    public State Action { get;  init;}
    public double Price { get; init; }
    public Cryptocurrency(State inAction, double inPrice) {
        Action = inAction;
        Price = inPrice;
    }
}