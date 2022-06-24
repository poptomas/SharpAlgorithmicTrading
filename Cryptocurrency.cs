enum State { Default, Buy, Sell, Hold };

struct Cryptocurrency {
    public State Action { get; private set;}
    public double Price { get; private set; }
    public Cryptocurrency(State inAction, double inPrice) {
        Action = inAction;
        Price = inPrice;
    }
}