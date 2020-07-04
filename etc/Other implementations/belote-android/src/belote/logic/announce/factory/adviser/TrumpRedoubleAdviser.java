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
import belote.logic.announce.factory.automat.methods.conditions.HasSquare;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.conditions.PartnerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.PlayerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.SequenceSuit;

/**
 * TrumpRedoubleAdviser class.
 * @author Dimitar Karamanov
 */
public final class TrumpRedoubleAdviser extends BaseRedoubleAdviser {

    /**
     * Constructor.
     * @param game Belote game instance.
     */
    public TrumpRedoubleAdviser(final Game game) {
        super(game);
        // Player made last announce
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new HasSuit(new SequenceSuit(SequenceType.Quint, Rank.Ace))));
        // Partner made last announce
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new HasSuit(new SequenceSuit(SequenceType.Quint, Rank.Ace))));
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new HasSquare()));
    }
}
