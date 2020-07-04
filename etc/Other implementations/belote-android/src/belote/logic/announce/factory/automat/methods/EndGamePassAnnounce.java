package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;
import belote.logic.announce.factory.automat.methods.conditions.PlayerTeamEndGameZone;

public class EndGamePassAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGamePassAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new OppositeTeamEndGameZone(game));
        addAnnounceCondition(new PlayerTeamEndGameZone());
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(final Player player) {
        return Announce.createPassAnnounce(player);
    }
}