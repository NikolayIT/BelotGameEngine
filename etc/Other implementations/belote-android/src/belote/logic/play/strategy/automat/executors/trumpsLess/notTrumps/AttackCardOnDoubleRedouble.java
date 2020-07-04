package belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps;

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
import belote.logic.play.strategy.automat.methods.trumpsLess.notTrump.PromoteTenRankCard;

class AttackCardOnDoubleRedouble extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public AttackCardOnDoubleRedouble(final Game game) {
        super(game);
        // Register play card methods.
        register(new MeterSuitCard(game));
        register(new TeamSuitPartnerCard(game));
        register(new HandCard(game));
        register(new PartnerSuitAnnounceCard(game));
        register(new PossiblePartnerSuitCard(game));
        register(new DominantSuitCard(game));
        register(new PromoteTenRankCard(game));
        register(new NeedlessCard(game));
    }

    /**
     * Handler method providing the user facility to check custom condition for methods executions.
     * @param player for which is called the executor
     * @return true to process method execution false to not.
     */
    protected boolean fitPreCondition(final Player player) {
        return game.getAnnounceList().getRedoubleAnnounce() != null || game.getAnnounceList().getDoubleAnnounce() != null;
    }
}

