/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions;

import belote.bean.Game;
import belote.logic.announce.factory.automat.methods.conditions.base.BaseTeamCanAnnounce;

/**
 * TeamCanAnnounce class.
 * @author Dimitar Karamanov
 */
public final class TeamCanAnnounce extends BaseTeamCanAnnounce {

    /**
     * Min points difference allowed to make end game announce.
     */
    private static final int MinPointsToMakeAnnounce = -6;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public TeamCanAnnounce(final Game game) {
        super(game, MinPointsToMakeAnnounce);
    }
}
