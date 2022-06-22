enum State { Default, Buy, Sell, Hold };

struct Cryptocurrency {
    public State Action { get; set;}
    public double Price { get; set; }
    public Cryptocurrency(State inAction, double inPrice) {
        Action = inAction;
        Price = inPrice;
    }
}