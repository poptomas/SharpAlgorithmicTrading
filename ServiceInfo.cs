using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    struct ServiceInfo {
        public double TradingFee {get; init;}
        public double DepositFee { get; init; }
        public double WithdrawalFee { get; init; }
        public int MinimumDeposit { get; init; }
        public string Currency { get; init; }
        public ServiceInfo(
            double inTradingFee, double inDepositFee, 
            double inWithdrawalFee, int inMinDeposit, string inCurrency) {
            TradingFee = inTradingFee;
            DepositFee = inDepositFee;
            WithdrawalFee = inWithdrawalFee;
            MinimumDeposit = inMinDeposit;
            Currency = inCurrency;
        }
    }
}
