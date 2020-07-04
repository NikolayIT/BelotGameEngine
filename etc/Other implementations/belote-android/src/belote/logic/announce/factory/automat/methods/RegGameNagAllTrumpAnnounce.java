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
import belote.bean.pack.card.rank.Rank;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.HasContractAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.TeamAdvance;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * RegGameNagAllTrumpAnnounce class. Announce factory method which creates regular nag all trump announce.
 * @author Dimitar Karamanov
 */
public final class RegGameNagAllTrumpAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNagAllTrumpAnnounce(final Game game) {
        super(game);
        addPreCondition(new HasContractAnnounce(game, AnnounceSuit.NotTrump));
        addAnnounceCondition(new RankCount(Rank.Jack, 4));
        addAnnounceCondition(new RankCount(Rank.Jack, 3));
        addAnnounceCondition(new MultipleAndCondition(new RankCount(Rank.Jack, 2), new RankCount(Rank.Nine, 2)));
        addAnnounceCondition(new MultipleAndCondition(new HasSuit(new SequenceSuit()), new RankCount(Rank.Jack, 2)));
        addAnnounceCondition(new MultipleAndCondition(new RankCount(Rank.Jack, 1), new RankCount(Rank.Nine, 3)));
        addAnnounceCondition(new MultipleAndCondition(new TeamAdvance(game), new RankCount(Rank.Jack, 2)));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        return Announce.createATNormalAnnounce(player);
    }
}
