﻿'use strict';
define(
    [
        'marionette',
        'Settings/MediaManagement/Naming/NamingSampleModel',
        'Mixins/AsModelBoundView'
    ], function (Marionette, NamingSampleModel, AsModelBoundView) {

        var view = Marionette.ItemView.extend({
            template: 'Settings/MediaManagement/Naming/NamingViewTemplate',

            ui: {
                namingOptions         : '.x-naming-options',
                renameEpisodesCheckbox: '.x-rename-episodes',
                singleEpisodeExample  : '.x-single-episode-example',
                multiEpisodeExample   : '.x-multi-episode-example'
            },

            events: {
                'change .x-rename-episodes': '_setFailedDownloadOptionsVisibility'
            },

            onRender: function () {
                if (!this.model.get('renameEpisodes')) {
                    this.ui.namingOptions.hide();
                }

                this.namingSampleModel = new NamingSampleModel();

                this.listenTo(this.model, 'change', this._updateSamples);
                this.listenTo(this.namingSampleModel, 'sync', this._showSamples);
                this._updateSamples();
            },

            _setFailedDownloadOptionsVisibility: function () {
                var checked = this.ui.renameEpisodesCheckbox.prop('checked');
                if (checked) {
                    this.ui.namingOptions.slideDown();
                }

                else {
                    this.ui.namingOptions.slideUp();
                }
            },

            _updateSamples: function () {
                this.namingSampleModel.fetch({ data: this.model.toJSON() });
            },

            _showSamples: function () {
                this.ui.singleEpisodeExample.html(this.namingSampleModel.get('singleEpisodeExample'));
                this.ui.multiEpisodeExample.html(this.namingSampleModel.get('multiEpisodeExample'));
            }
        });

        return AsModelBoundView.call(view);
    });
