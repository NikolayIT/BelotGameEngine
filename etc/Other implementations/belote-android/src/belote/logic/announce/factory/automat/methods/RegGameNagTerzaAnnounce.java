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
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.SuitCount;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * RegGameNagTerzaAnnounce class. Announce factory method which creates an suit announce when has terza.
 * @author Dimitar Karamanov
 */
public final class RegGameNagTerzaAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNagTerzaAnnounce(final Game game) {
        super(game);
        // Only terza
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.King)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Queen)));
        addAnnounceCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Jack)));
        // Terza and one more from same suit
        addAnnounceCondition(new SuitCount(new SequenceSuit(SequenceType.Tierce, Rank.Ace), 4));
        // Terza and one more Ace
        addAnnounceCondition(new MultipleAndCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Nine)), new RankCount(Rank.Ace, 1)));
        addAnnounceCondition(new MultipleAndCondition(new HasSuit(new SequenceSuit(SequenceType.Tierce, Rank.Ten)), new RankCount(Rank.Ace, 1)));
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