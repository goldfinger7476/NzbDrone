﻿﻿'use strict';
define(
    [
        'marionette',
        'Mixins/AsModelBoundView'
    ], function (Marionette, AsModelBoundView) {

        var view = Marionette.ItemView.extend({
            template: 'Settings/MediaManagement/Sorting/ViewTemplate'
        });

        return AsModelBoundView.call(view);
    });
