import React, {FC, useState} from "react";
import clsx from "clsx";
import styles from "./Toolbar.module.pcss";
import {BiSolidEdit} from "react-icons/bi";
import Input from "../../../components/elements/Input.tsx";
import {FaChevronDown, FaSearch} from "react-icons/fa";
import {FaPlus} from "react-icons/fa6";

const Toolbar: FC = () => {
    const [expanded, setExpanded] = useState(false);
    return (
        <div className="w-full mt-[7px] h-[40px] relative">
            <div className={clsx(styles.wrapper, expanded && "h-[110px]")}>

                <button className={styles.editButton}>
                    <BiSolidEdit size={20}/>
                </button>
                <div className="w-full flex-center relative -order-1 z-20  h-[40px]">
                    <div className={clsx(styles.searchOverlay, expanded || "h-0")}></div>
                    <Input className="rounded-lg" placeholder="Search in accounts"/>
                    <div className={styles.searchPanel}>

                        <FaChevronDown size={15}
                                       className={clsx("mr-2 text-fg-2 transition-transform sm:hidden", expanded && "rotate-180")}
                                       onClick={() => setExpanded(prev => !prev)}/>

                        <div className={styles.iconWrapper}>
                            <FaSearch/>
                        </div>
                    </div>
                </div>
                <button className={styles.addButton}>
                    <FaPlus size={20}/>
                </button>
            </div>
        </div>
    )
}

export default React.memo(Toolbar);