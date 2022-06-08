enum State { DEFAULT, BUY, SELL, HOLD };

struct Cryptocurrency {
    public State Action {get; set;}
    public State Price { get; set; }
}