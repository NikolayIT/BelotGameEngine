package belote.logic.announce.factory.automat.methods.conditions.base;

import belote.bean.Game;

public abstract class TeamEndGameZone implements AnnounceCondition {

    private static final int MINIMUM_OFFSET = 13;

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public final boolean process(final int teamPoints) {
        return (Game.END_GAME_POINTS - teamPoints) <= MINIMUM_OFFSET;
    }
}
