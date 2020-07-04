/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.adviser;

import belote.bean.Game;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.sequence.SequenceType;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.conditions.PartnerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.PlayerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * AllTrumpsRedoubleAdviser class.
 * @author Dimitar Karamanov
 */
public final class AllTrumpsRedoubleAdviser extends BaseRedoubleAdviser {

    /**
     * Constructor.
     * @param game Belote game instance.
     */
    public AllTrumpsRedoubleAdviser(final Game game) {
        super(game);
        // Player made last announce
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new RankCount(Rank.Jack, 4)));
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new RankCount(Rank.Jack, 1), new RankCount(Rank.Nine, 4)));
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new RankCount(Rank.Jack, 3), new RankCount(Rank.Nine, 1)));
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new HasSuit(new SequenceSuit(SequenceType.Quint, Rank.Ace))));
        // Partner made last announce
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new RankCount(Rank.Jack, 2)));
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new RankCount(Rank.Jack, 1), new RankCount(Rank.Nine, 1)));
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new RankCount(Rank.Nine, 3)));
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new HasSuit(new SequenceSuit(SequenceType.Quint))));
    }
}
