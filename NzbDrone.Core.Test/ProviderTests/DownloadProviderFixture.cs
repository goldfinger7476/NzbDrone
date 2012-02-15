﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Providers.DownloadClients;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;

// ReSharper disable InconsistentNaming

namespace NzbDrone.Core.Test.ProviderTests
{
    [TestFixture]
    public class DownloadProviderFixture : CoreTest
    {

        private void SetDownloadClient(DownloadClientType clientType)
        {
            Mocker.GetMock<ConfigProvider>()
                 .Setup(c => c.DownloadClient)
                 .Returns(clientType);
        }

        private EpisodeParseResult SetupParseResult()
        {
            var episodes = Builder<Episode>.CreateListOfSize(2)
                            .TheFirst(1).With(s => s.EpisodeId = 12)
                            .TheNext(1).With(s => s.EpisodeId = 99)
                            .All().With(s => s.SeriesId = 5)
                            .Build().ToList();

            Mocker.GetMock<EpisodeProvider>()
                    .Setup(c => c.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(episodes);

            return Builder<EpisodeParseResult>.CreateNew()
                .With(c => c.Quality = new Quality(QualityTypes.DVD, false))
                .With(c => c.Series = Builder<Series>.CreateNew().Build())
                .With(c => c.EpisodeNumbers = new List<int>{2})
                .Build();
        }

        private void WithSuccessfullAdd()
        {
            Mocker.GetMock<SabProvider>()
                .Setup(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()))
                .Returns(true);

            Mocker.GetMock<BlackholeProvider>()
                .Setup(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()))
                .Returns(true);
        }

        private void WithFailedAdd()
        {
            Mocker.GetMock<SabProvider>()
                .Setup(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<BlackholeProvider>()
                .Setup(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()))
                .Returns(false);
        }


        [Test]
        public void Download_report_should_send_to_sab_add_to_history_mark_as_grabbed()
        {
            WithSuccessfullAdd();
            SetDownloadClient(DownloadClientType.Sabnzbd);

            var parseResult = SetupParseResult();

            //Act
            Mocker.Resolve<DownloadProvider>().DownloadReport(parseResult);


            //Assert
            Mocker.GetMock<SabProvider>()
                .Verify(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()), Times.Once());

            Mocker.GetMock<BlackholeProvider>()
                .Verify(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()), Times.Never());

            Mocker.GetMock<HistoryProvider>()
                .Verify(s => s.Add(It.Is<History>(h => h.EpisodeId == 12 && h.SeriesId == 5)), Times.Once());

            Mocker.GetMock<HistoryProvider>()
                .Verify(s => s.Add(It.Is<History>(h => h.EpisodeId == 99 && h.SeriesId == 5)), Times.Once());

            Mocker.GetMock<EpisodeProvider>()
                .Verify(c => c.MarkEpisodeAsFetched(12));

            Mocker.GetMock<EpisodeProvider>()
                .Verify(c => c.MarkEpisodeAsFetched(99));

            Mocker.GetMock<ExternalNotificationProvider>()
                .Verify(c => c.OnGrab(It.IsAny<string>()));
        }

        [Test]
        public void should_download_nzb_to_blackhole_add_to_history_mark_as_grabbed()
        {
            WithSuccessfullAdd();
            SetDownloadClient(DownloadClientType.Blackhole);

            var parseResult = SetupParseResult();

            //Act
            Mocker.Resolve<DownloadProvider>().DownloadReport(parseResult);


            //Assert
            Mocker.GetMock<SabProvider>()
                .Verify(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()), Times.Never());

            Mocker.GetMock<BlackholeProvider>()
                .Verify(s => s.DownloadNzb(It.IsAny<String>(), It.IsAny<String>()), Times.Once());

            Mocker.GetMock<HistoryProvider>()
                .Verify(s => s.Add(It.Is<History>(h => h.EpisodeId == 12 && h.SeriesId == 5)), Times.Once());

            Mocker.GetMock<HistoryProvider>()
                .Verify(s => s.Add(It.Is<History>(h => h.EpisodeId == 99 && h.SeriesId == 5)), Times.Once());

            Mocker.GetMock<EpisodeProvider>()
                .Verify(c => c.MarkEpisodeAsFetched(12));

            Mocker.GetMock<EpisodeProvider>()
                .Verify(c => c.MarkEpisodeAsFetched(99));

            Mocker.GetMock<ExternalNotificationProvider>()
                .Verify(c => c.OnGrab(It.IsAny<string>()));
        }

        [TestCase(DownloadClientType.Sabnzbd)]
        [TestCase(DownloadClientType.Blackhole)]
        public void Download_report_should_not_add_to_history_mark_as_grabbed_if_add_fails(DownloadClientType clientType)
        {
            WithFailedAdd();
            SetDownloadClient(clientType);

            var parseResult = SetupParseResult();

            //Act
            Mocker.Resolve<DownloadProvider>().DownloadReport(parseResult);

            Mocker.GetMock<HistoryProvider>()
                .Verify(s => s.Add(It.IsAny<History>()), Times.Never());


            Mocker.GetMock<EpisodeProvider>()
                .Verify(c => c.MarkEpisodeAsFetched(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<ExternalNotificationProvider>()
                .Verify(c => c.OnGrab(It.IsAny<String>()), Times.Never());
        }



        [Test]
        public void should_return_sab_as_active_client()
        {
            SetDownloadClient(DownloadClientType.Sabnzbd);
            Mocker.Resolve<DownloadProvider>().GetActiveDownloadClient().Should().BeAssignableTo<SabProvider>();
        }

        [Test]
        public void should_return_blackhole_as_active_client()
        {
            SetDownloadClient(DownloadClientType.Blackhole);
            Mocker.Resolve<DownloadProvider>().GetActiveDownloadClient().Should().BeAssignableTo<BlackholeProvider>();
        }


        [TestCase(1, new[] { 2 }, "My Episode Title", QualityTypes.DVD, false, Result = "My Series Name - 1x2 - My Episode Title [DVD]")]
        [TestCase(1, new[] { 2 }, "My Episode Title", QualityTypes.DVD, true, Result = "My Series Name - 1x2 - My Episode Title [DVD] [Proper]")]
        [TestCase(1, new[] { 2 }, "", QualityTypes.DVD, true, Result = "My Series Name - 1x2 -  [DVD] [Proper]")]
        [TestCase(1, new[] { 2, 4 }, "My Episode Title", QualityTypes.HDTV, false, Result = "My Series Name - 1x2-1x4 - My Episode Title [HDTV]")]
        [TestCase(1, new[] { 2, 4 }, "My Episode Title", QualityTypes.HDTV, true, Result = "My Series Name - 1x2-1x4 - My Episode Title [HDTV] [Proper]")]
        [TestCase(1, new[] { 2, 4 }, "", QualityTypes.HDTV, true, Result = "My Series Name - 1x2-1x4 -  [HDTV] [Proper]")]
        public string create_proper_sab_titles(int seasons, int[] episodes, string title, QualityTypes quality, bool proper)
        {
            var series = Builder<Series>.CreateNew()
                    .With(c => c.Title = "My Series Name")
                    .Build();

            var parsResult = new EpisodeParseResult()
            {
                AirDate = DateTime.Now,
                EpisodeNumbers = episodes.ToList(),
                Quality = new Quality(quality, proper),
                SeasonNumber = seasons,
                Series = series,
                EpisodeTitle = title
            };

            return Mocker.Resolve<DownloadProvider>().GetDownloadTitle(parsResult);
        }

        [TestCase(true, Result = "My Series Name - Season 1 [Bluray720p] [Proper]")]
        [TestCase(false, Result = "My Series Name - Season 1 [Bluray720p]")]
        public string create_proper_sab_season_title(bool proper)
        {
            var series = Builder<Series>.CreateNew()
                                .With(c => c.Title = "My Series Name")
                                .Build();

            var parsResult = new EpisodeParseResult()
            {
                AirDate = DateTime.Now,
                Quality = new Quality(QualityTypes.Bluray720p, proper),
                SeasonNumber = 1,
                Series = series,
                EpisodeTitle = "My Episode Title",
                FullSeason = true
            };

            return Mocker.Resolve<DownloadProvider>().GetDownloadTitle(parsResult);
        }

        [TestCase(true, Result = "My Series Name - 2011-12-01 - My Episode Title [Bluray720p] [Proper]")]
        [TestCase(false, Result = "My Series Name - 2011-12-01 - My Episode Title [Bluray720p]")]
        public string create_proper_sab_daily_titles(bool proper)
        {
            var series = Builder<Series>.CreateNew()
                    .With(c => c.IsDaily = true)
                    .With(c => c.Title = "My Series Name")
                    .Build();

            var parsResult = new EpisodeParseResult
            {
                AirDate = new DateTime(2011, 12, 1),
                Quality = new Quality(QualityTypes.Bluray720p, proper),
                Series = series,
                EpisodeTitle = "My Episode Title",
            };

            return Mocker.Resolve<DownloadProvider>().GetDownloadTitle(parsResult);
        }

    }
}