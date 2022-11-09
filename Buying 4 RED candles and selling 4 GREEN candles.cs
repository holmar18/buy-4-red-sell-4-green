using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

/*
    https://www.youtube.com/watch?v=WE7aOUyxRJc

*/
namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class Buying4REDcandlesandselling4GREENcandles : Robot
    {
        #region Inputs
        
        [Parameter("Candles in a row for valid signal", DefaultValue = 4, MinValue = 2, Step = 1, Group = "Strategy Settings")]
        public int CandlesInARow { get; set; }
        
        [Parameter("Lots", DefaultValue = 1, MinValue = 0.1, Step = 0.1, Group = "Risk Settings")]
        public double Lots { get; set; }
        
        [Parameter("TakeProfit (Forex=Pips, Stocks=Cents)", DefaultValue = 500, MinValue = 1, Step = 1, Group = "Risk Settings")]
        public double TakeProfit { get; set; }
        
        [Parameter("Stoploss (Forex=Pips, Stocks=Cents)", DefaultValue = 500, MinValue = 1, Step = 1, Group = "Risk Settings")]
        public double StopLoss { get; set; }
        
        [Parameter("Source", Group = "EMA settings")]
        public DataSeries EmaSrc { get; set; }
        
        [Parameter("Periods", DefaultValue = 10, MinValue = 1, MaxValue = 500, Step = 1, Group = "EMA settings")]
        public int EmaPeriods { get; set; }
        
        [Parameter("Draw EMA", DefaultValue = false, Group = "EMA settings")]
        public bool DrawEmaB { get; set; }
        
        [Parameter("Use Martingale", DefaultValue = false, Group = "Martingale settings")]
        public bool UseMartingGale { get; set; }
        
        [Parameter("Martingale multiplier", DefaultValue = 2, MinValue = 1, Step = 1, Group = "Martingale settings")]
        public double MartinGaleMlp { get; set; }
        #endregion


        #region Variables
        private int CandleCountShort = 0;
        private int CandleCountLong = 0;
        private double EMA;
        private int LastIndex;
        private int LastEmaDrawIndex;
        private double MartinGaleVol;
        private bool MartinGaleNext = false;
        #endregion

        protected override void OnStart()
        {
            Positions.Closed += OnClosePos;
        }

        protected override void OnTick()
        {
            EMA = Indicators.ExponentialMovingAverage(EmaSrc, EmaPeriods).Result.LastValue;
            DrawEma();
            

            ExecuteTrade();
            CountCandlesInRow();


            
            CheckIfClosePos();
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }

        #region Strategy
        private void CountCandlesInRow()
        {
            int index = Bars.Count - 1;
            double open = Bars.OpenPrices[index];
            double close = Bars.ClosePrices[index];
            
            int pc = Positions.Count;

            if(pc == 0 & close > open & LastIndex != index & close > EMA)
            {
                CandleCountLong += 1;
                CandleCountShort = 0;
                LastIndex = index;
                ColorCandlesInArow(Color.Orange);
            }
            else if(pc == 0 & open > close & LastIndex != index & close < EMA)
            {
                CandleCountShort += 1;
                CandleCountLong = 0;
                LastIndex = index;
                ColorCandlesInArow(Color.DeepPink);
            }
            

            
            
        }
        #endregion


        #region Coloring
        private void ColorCandlesInArow(Color col)
        {
           if(CandleCountLong == CandlesInARow & Positions.Count == 0 | CandleCountShort == CandlesInARow & Positions.Count == 0)
           {
                for(var i = CandlesInARow; i > 0; i --)
                {
                    Chart.SetBarColor(Bars.Count - i, col);
                }
           }
        }
        
        private void DrawEma()
        {
            if(DrawEmaB & LastEmaDrawIndex != Bars.Count - 1)
            {
                Chart.DrawText(string.Format("{0}", RandomNum()), "o", Bars.Count - 1, EMA, Color.Blue);
                LastEmaDrawIndex = Bars.Count - 1;
            }
        }
        
        #endregion


        #region Helpers
        private int RandomNum()
        {
            Random r = new Random();
            return r.Next(0, 1000000);
        }
        #endregion


        #region Trade
        private void ExecuteTrade()
        {
            if(Positions.Count > 0)
            {
                return;
            }
            
            double volume = MartinGaleNext ? Symbol.NormalizeVolumeInUnits(MartinGaleVol) : Symbol.NormalizeVolumeInUnits(Lots * 100000);
            
            if(CandleCountLong == CandlesInARow)
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol.Name, volume, "Short", StopLoss, TakeProfit);
                MartinGaleNext = false;
            }
            else if(CandleCountShort == CandlesInARow)
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volume, "Long", StopLoss, TakeProfit);
                MartinGaleNext = false;
            }
        }
        
        
        private void CheckIfClosePos()
        {
            if(Positions.Count == 0)
            {
                return;
            }
            double close = Bars.ClosePrices[Bars.Count - 1];
            
            if(CandleCountLong == CandlesInARow & close < EMA)
            {
                ClosePosition(Positions.First());
                CandleCountLong = 0;
                CandleCountShort = 0;
            }
            else if(CandleCountShort == CandlesInARow & close > EMA)
            {
                ClosePosition(Positions.First());
                CandleCountShort = 0;
                CandleCountLong = 0;
            }
        }
        
        
        private void OnClosePos(PositionClosedEventArgs args)
        {
            if(UseMartingGale)
            {
                if(args.Position.GrossProfit < 0)
                {
                    Print("Martingale NEXT");
                    MartinGaleVol = args.Position.VolumeInUnits * MartinGaleMlp;
                    Print("Mart volume: ", MartinGaleVol);
                    MartinGaleNext = true;
                }
            }
        }
        #endregion
    }
}