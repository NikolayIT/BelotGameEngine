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
import belote.logic.announce.factory.automat.methods.EndGameNormalEnemyTeamEndGameZoneAnnounce;
import belote.logic.announce.factory.automat.methods.EndGameNormalPlayerTeamEndGameZoneAnnounce;
import belote.logic.announce.factory.automat.methods.EndGameRedoublelEnemyTeamEndGameZoneAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.HasContractAnnounce;

/**
 * EndGameNormalAnnounce class.
 * @author Dimitar Karamanov
 */
public final class EndGameNormalAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameNormalAnnounce(final Game game) {
        super(game);
        // Precondition
        addPreCondition(new HasContractAnnounce(game));
        // Methods
        register(new EndGameRedoublelEnemyTeamEndGameZoneAnnounce(game));
        register(new EndGameNormalEnemyTeamEndGameZoneAnnounce(game));
        register(new EndGameNormalPlayerTeamEndGameZoneAnnounce(game));
    }
}
