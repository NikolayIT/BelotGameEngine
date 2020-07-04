/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors;

import belote.bean.Game;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;
import belote.logic.play.strategy.automat.methods.LastCleanedSuitCard;
import belote.logic.play.strategy.automat.methods.NoCleanedSuitCard;
import belote.logic.play.strategy.automat.methods.NoCleanedSuitNoEnemyPreferredCard;
import belote.logic.play.strategy.automat.methods.PartnerJackAceSuitCard;
import belote.logic.play.strategy.automat.methods.PartnerPreferredSuitCard;
import belote.logic.play.strategy.automat.methods.TeamSuitCard;

/**
 * PartnerPossibleSuitCard class. Used in XXXAttackCard executors.
 * @author Dimitar Karamanov
 */
public final class PossiblePartnerSuitCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public PossiblePartnerSuitCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new TeamSuitCard(game));
        register(new PartnerJackAceSuitCard(game));
        register(new PartnerPreferredSuitCard(game));
        register(new NoCleanedSuitNoEnemyPreferredCard(game));
        register(new LastCleanedSuitCard(game));
        register(new NoCleanedSuitCard(game));
    }
}
