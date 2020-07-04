/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.suit.AnnounceSuit;
import belote.logic.announce.factory.adviser.AllTrumpsRedoubleAdviser;
import belote.logic.announce.factory.adviser.TrumpRedoubleAdviser;
import belote.logic.announce.factory.adviser.NoTrumpsRedoubleAdviser;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamDoubleAnnounce;

/**
 * RedoubleAnnounce class. Announce factory method which creates a redouble announce.
 * @author Dimitar Karamanov
 */
public final class RedoubleAnnounce extends ConditionListMethod {

    /**
     * Not trump redouble adviser.
     */
    private final NoTrumpsRedoubleAdviser ntRedoubleAdviser;

    /**
     * All trump redouble adviser.
     */
    private final AllTrumpsRedoubleAdviser atRedoubleAdviser;

    /**
     * Trump suit redouble adviser.
     */
    private final TrumpRedoubleAdviser clRedoubleAdviser;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RedoubleAnnounce(final Game game) {
        super(game);
        ntRedoubleAdviser = new NoTrumpsRedoubleAdviser(game);
        atRedoubleAdviser = new AllTrumpsRedoubleAdviser(game);
        clRedoubleAdviser = new TrumpRedoubleAdviser(game);

        addAnnounceCondition(new OppositeTeamDoubleAnnounce(game));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();

        if (announce != null) {
            if (announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump)) {
                return ntRedoubleAdviser.getAnnounce(player);
            }

            if (announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump)) {
                return atRedoubleAdviser.getAnnounce(player);
            }

            return clRedoubleAdviser.getAnnounce(player);
        }

        return null;
    }
}
