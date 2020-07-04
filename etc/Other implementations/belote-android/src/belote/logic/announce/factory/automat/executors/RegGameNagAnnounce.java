/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.executors;

import belote.bean.Game;
import belote.logic.announce.factory.automat.executors.base.AnnounceExecutor;
import belote.logic.announce.factory.automat.methods.RegGameNagAllTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameNagAllTrumpDealAttackHasJackSuitAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameNagAllTrumpDealAttackHasSequenceAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameNagNoTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameNagNoTrumpWhenFirstAndHasAceSuitAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamNormalAnnounce;

/**
 * RegGameNagAnnounce class.
 * @author Dimitar Karamanov
 */
public final class RegGameNagAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNagAnnounce(final Game game) {
        super(game);
        // Preconditions
        addPreCondition(new OppositeTeamNormalAnnounce(game));
        // Methods
        register(new RegGameNagAllTrumpDealAttackHasSequenceAnnounce(game));
        register(new RegGameNagAllTrumpDealAttackHasJackSuitAnnounce(game));
        register(new RegGameNagAllTrumpAnnounce(game));
        register(new RegGameNagNoTrumpAnnounce(game));
        register(new RegGameNagNoTrumpWhenFirstAndHasAceSuitAnnounce(game));
        register(new RegGameNagTrumpAnnounce(game));
    }
}