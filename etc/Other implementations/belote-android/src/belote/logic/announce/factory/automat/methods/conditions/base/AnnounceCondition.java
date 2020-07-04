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
 * AnnounceCondition interface. Defines a single condition which is used direct or as component of complicated condition.
 * @author Dimitar Karamanov
 */
public interface AnnounceCondition {

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    boolean process(Player player);
}
