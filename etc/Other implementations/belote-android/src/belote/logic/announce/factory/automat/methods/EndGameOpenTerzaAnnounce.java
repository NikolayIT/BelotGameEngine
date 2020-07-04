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
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.sequence.SequenceType;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * EndGameOpenTerzaAnnounce class. Announce factory method which creates normal suit announce when the sequence involves jack card.
 * @author Dimitar Karamanov
 */
public final class EndGameOpenTerzaAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameOpenTerzaAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.King)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Queen)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Jack)));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        final Suit suit = SequenceSuit.getSequenceSuit(player);
        return Announce.createSuitNormalAnnounce(player, suit);
    }
}
