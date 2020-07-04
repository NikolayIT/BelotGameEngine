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
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.sequence.SequenceType;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.TeamAttack;
import belote.logic.announce.factory.automat.methods.conditions.TeamDefence;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.JackNineAceSuit;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * RegGameSupportAllTrumpAnnounce class. Announce factory method which creates support all trump announce.
 * @author Dimitar Karamanov
 */
public final class RegGameSupportAllTrumpAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameSupportAllTrumpAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Quint)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Quarte, Rank.Ace)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Quarte, Rank.King)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Quarte, Rank.Queen)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Quarte, Rank.Jack)));

        addAnnounceCondition(new MultipleAndCondition(new TeamAttack(game), new RankCount(Rank.Jack, 2)));
        addAnnounceCondition(new MultipleAndCondition(new TeamAttack(game), new RankCount(Rank.Jack, 1), new RankCount(Rank.Nine, 1)));
        addAnnounceCondition(new MultipleAndCondition(new TeamAttack(game), new RankCount(Rank.Nine, 3)));

        addAnnounceCondition(new MultipleAndCondition(new TeamDefence(game), new RankCount(Rank.Jack, 2)));
        addAnnounceCondition(new MultipleAndCondition(new TeamDefence(game), new RankCount(Rank.Jack, 1), new RankCount(Rank.Nine, 2)));
        addAnnounceCondition(new MultipleAndCondition(new TeamDefence(game), new HasSuit(new JackNineAceSuit())));
        addAnnounceCondition(new MultipleAndCondition(new TeamDefence(game), new RankCount(Rank.Nine, 4)));
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