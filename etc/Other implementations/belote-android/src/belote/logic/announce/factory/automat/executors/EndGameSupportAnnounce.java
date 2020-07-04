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
import belote.logic.announce.factory.automat.methods.EndGameSupportAllTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;
import belote.logic.announce.factory.automat.methods.conditions.PartnerNormalTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.PlayerTeamEndGameZone;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleOrCondition;

/**
 * EndGameSupportAnnounce class.
 * @author Dimitar Karamanov
 */
public final class EndGameSupportAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameSupportAnnounce(final Game game) {
        super(game);
        // Preconditions
        addPreCondition(new MultipleOrCondition(new OppositeTeamEndGameZone(game), new PlayerTeamEndGameZone()));
        addPreCondition(new PartnerNormalTrumpAnnounce(game));
        // Methods
        register(new EndGameSupportTrumpAnnounce(game));
        register(new EndGameSupportAllTrumpAnnounce(game));
        //register(new EndGameSupportNoTrumpAnnounce(game));
    }
}