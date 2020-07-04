/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions.base;

import belote.bean.Player;

/**
 * NotCondition class. NotCondition is an complicated condition which reverts the result of other announce condition.
 * @author Dimitar Karamanov
 */
public final class NotCondition implements AnnounceCondition {

    private final AnnounceCondition condition;

    /**
     * Constructor.
     * @param condition used to be reverted.
     */
    public NotCondition(final AnnounceCondition condition) {
        this.condition = condition;
    }

    /**
     * The method which returns the result of condition. (Boolean NOT)
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        return !condition.process(player);
    }
}
