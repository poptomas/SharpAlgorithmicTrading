enum State { Default, Buy, Sell, Hold };

struct Cryptocurrency {
    public State Action {get; set;}
    public State Price { get; set; }
}