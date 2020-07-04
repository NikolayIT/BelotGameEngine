/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors.trumpsLess.allTrumps;

import belote.bean.Game;
import belote.bean.Player;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;
import belote.logic.play.strategy.automat.executors.PossiblePartnerSuitCard;
import belote.logic.play.strategy.automat.executors.trumpsLess.NeedlessCard;
import belote.logic.play.strategy.automat.methods.MeterSuitCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.DominantSuitCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.HandCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.PartnerSuitAnnounceCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.TeamSuitPartnerCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.allTrump.BelotCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.allTrump.PromoteNineRankCard;

/**
 * Partner declared AllTrumpAttackCard executor. Used in AllTrumpAttackCard getAttackCard().
 * @author Dimitar Karamanov
 */
class AttackCardOnPartnerContract extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public AttackCardOnPartnerContract(final Game game) {
        super(game);
        // Register play card methods.
        register(new MeterSuitCard(game));
        register(new HandCard(game));
        register(new TeamSuitPartnerCard(game));
        register(new PartnerSuitAnnounceCard(game));
        register(new PossiblePartnerSuitCard(game));
        register(new DominantSuitCard(game));
        register(new PromoteNineRankCard(game));
        register(new BelotCard(game));
        register(new NeedlessCard(game));
    }

    /**
     * Handler method providing the user facility to check custom condition for methods executions.
     * @param player for which is called the executor
     * @return true to process method execution false to not.
     */
    protected boolean fitPreCondition(final Player player) {
        final Player partner = player.getPartner();

        return partner.equals(game.getAnnounceList().getOpenContractAnnouncePlayer());
    }
}
