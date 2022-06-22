using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal struct ServiceNumerics {
        public double TradingFee {get; init;}
        public double WithdrawalFee { get; init; }
        public int MinimumDeposit { get; init; }
        public ServiceNumerics(double inTradingFee, double inWithdrawalFee, int inMinDeposit) {
            TradingFee = inTradingFee;
            WithdrawalFee = inWithdrawalFee;
            MinimumDeposit = inMinDeposit;
        }
    }
}
