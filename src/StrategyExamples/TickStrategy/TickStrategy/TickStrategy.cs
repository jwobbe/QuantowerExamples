// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace TickStrategy
{
    /// <summary>
    /// An example of strategy for working with one symbol. Add your code, compile it and run via Strategy Runner panel in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// </summary>
    public class TickStrategy : Strategy, ICurrentSymbol
    {
        private HistoricalData _historicalData;

        [InputParameter("Symbol", 10)]
        public Symbol CurrentSymbol { get; set; }

        [InputParameter("Account", 20)]
        public Account Account { get; set; }

        [InputParameter("Tick Count", 20)]
        public int TickCount { get; set; } = 233;


        public override string[] MonitoringConnectionsIds => new string[] { this.CurrentSymbol?.ConnectionId };

        public TickStrategy()
          : base()
        {
            // Defines strategy's name and description.
            this.Name = "TickStrategy";
            this.Description = "My strategy's annotation";
        }

        /// <summary>
        /// This function will be called after creating a strategy
        /// </summary>
        protected override void OnCreated()
        {
            // Add your code here
        }

        /// <summary>
        /// This function will be called after running a strategy
        /// </summary>
        protected override void OnRun()
        {
            if (CurrentSymbol == null || Account == null || CurrentSymbol.ConnectionId != Account.ConnectionId)
            {
                Log("Incorrect input parameters... Symbol or Account are not specified or they have diffent connectionID.", StrategyLoggingLevel.Error);
                return;
            }

            CurrentSymbol = Core.GetSymbol(this.CurrentSymbol?.CreateInfo());
            var aggregation = new HistoryAggregationTick(TickCount);
            var fromTime = FindFuturesWeekStartBefore(Core.TimeUtils.DateTimeUtcNow);
            Log($"History starting at {fromTime}");

            _historicalData = CurrentSymbol.GetHistory(new HistoryRequestParameters
            {
                Aggregation = aggregation,
                FromTime = fromTime,
                HistoryType = HistoryType.Last,
                UsePrevCloseAsOpenPriceBar = false,
            });

            _historicalData.NewHistoryItem += HistoricalData_NewHistoryItem;
            _historicalData.HistoryItemUpdated += HistoricalData_HistoryItemUpdated;
        }

        private void HistoricalData_HistoryItemUpdated(object sender, HistoryEventArgs e)
        {
            //Log($"Ticks Remaining: {TickCount - e.HistoryItem[PriceType.Ticks]}");
        }

        /// <summary>
        /// This function will be called after stopping a strategy
        /// </summary>
        protected override void OnStop()
        {
            if (_historicalData != null)
            {
                _historicalData.NewHistoryItem -= HistoricalData_NewHistoryItem;
                _historicalData.Dispose();
            }
        }

        /// <summary>
        /// This function will be called after removing a strategy
        /// </summary>
        protected override void OnRemove()
        {
            this.CurrentSymbol = null;
            this.Account = null;
            // Add your code here
        }

        /// <summary>
        /// Use this method to provide run time information about your strategy. You will see it in StrategyRunner panel in trading terminal
        /// </summary>
        protected override List<StrategyMetric> OnGetMetrics()
        {
            List<StrategyMetric> result = base.OnGetMetrics();

            // An example of adding custom strategy metrics:
            // result.Add("Opened buy orders", "2");
            // result.Add("Opened sell orders", "7");

            return result;
        }

        private DateTime FindFuturesWeekStartBefore(DateTime dateTime)
        {
            var weeklyStartDate = dateTime;
            while (weeklyStartDate.DayOfWeek != DayOfWeek.Sunday)
            {
                weeklyStartDate = weeklyStartDate.AddDays(-1);
            }

            weeklyStartDate = new DateTime(weeklyStartDate.Year, weeklyStartDate.Month, weeklyStartDate.Day, 17, 0, 0);
            var futuresExchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var weeklyStartDateOffsetFuturesExchange = new DateTimeOffset(weeklyStartDate, futuresExchangeTimeZone.GetUtcOffset(weeklyStartDate));
            return weeklyStartDateOffsetFuturesExchange.UtcDateTime;
        }

        private void HistoricalData_NewHistoryItem(object sender, HistoryEventArgs e)
        {
            Log($"New candle started at {DateTime.Now} with an open price of {e.HistoryItem[PriceType.Open]}");
        }
    }
}