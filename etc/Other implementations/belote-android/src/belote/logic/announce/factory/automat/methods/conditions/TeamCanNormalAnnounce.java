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
import belote.bean.announce.suit.AnnounceSuit;
import belote.logic.announce.factory.automat.methods.conditions.base.BaseTeamCanAnnounce;

/**
 * TeamCanNormalAnnounce class.
 * @author Dimitar Karamanov
 */
public final class TeamCanNormalAnnounce extends BaseTeamCanAnnounce {

    /**
     * Minimum points to make normal announce (can hold double announce).
     */
    private static final int MinPointsToMakeNormalAnnounce = AnnounceSuit.AllTrump.getBasePoints() * 2;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public TeamCanNormalAnnounce(final Game game) {
        super(game, MinPointsToMakeNormalAnnounce);
    }
}
