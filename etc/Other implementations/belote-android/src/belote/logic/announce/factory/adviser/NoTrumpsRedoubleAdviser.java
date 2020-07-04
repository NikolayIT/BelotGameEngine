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
import belote.logic.announce.factory.automat.methods.conditions.PartnerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.PlayerRegularAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;

/**
 * NoTrumpsRedoubleAdviser class.
 * @author Dimitar Karamanov
 */
public final class NoTrumpsRedoubleAdviser extends BaseRedoubleAdviser {

    /**
     * Constructor.
     * @param game Belote game instance
     */
    public NoTrumpsRedoubleAdviser(final Game game) {
        super(game);
        // Player made last announce
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new RankCount(Rank.Ace, 4)));
        addAnnounceCondition(new MultipleAndCondition(new PlayerRegularAnnounce(game), new RankCount(Rank.Ace, 3), new RankCount(Rank.Ten, 1)));
        // Partner made last announce
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new RankCount(Rank.Ace, 1)));
        addAnnounceCondition(new MultipleAndCondition(new PartnerRegularAnnounce(game), new RankCount(Rank.Ten, 2)));
    }
}
