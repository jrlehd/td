using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    PanelRenderer renderer;         //Inspecter에서 볼 수 있는 애
    VisualElement slotContainer;    //UI툴킷 창을 열어야 보이는 애들
    Label itemInfoLable;            //UI툴킷 창을 열어야 보이는 애들

    //인벤토리 구조화
    public int totalSlotCount = 12; //인벤토리 총 칸수
    public List<InventoryItem> initItems = new List<InventoryItem>(); //초기 아이템
    //리스트에서 자료의 수를 제는 방법 List.Count
    InventoryItem[] slots; //아이템 칸
    bool isInit = false; //초기화가 된 인벤토리 인지를 확인, Scene 전환이나 Save/Load시에도 isinit의 현재 상태는 바뀌면 안됨

    VisualElement root; //드랍포인트 보정용
    VisualElement dragGhostIcon; //고스트아이콘(무언가를 마우스로 옮길 때 나오는 반투명한 아이콘)
    bool isDragging = false;    //클릭 이후 클릭을 떼기 전까지 구별용
    int dragStartIndex = -1;    //클릭된 아이템의 번호 저장용
    int dragEndIndex = -1;      //내려놓은 아이템의 번호 저장용


    //슬롯 초기화 method
    void InitSlotData()
    {
        if (isInit == true)
        {
            return;
        }

        slots = new InventoryItem[totalSlotCount]; //인벤토리 총 칸수 구현
        for (int i = 0; i < totalSlotCount; i++)
        {
            if (i < initItems.Count && initItems[i] != null)
            {
                //초기 지급 아이템을 빈 인벤토리에 저장
                slots[i] = initItems[i].Clone(); //주소 참조로 인한 공멸 방지(사용자가 초기 아이템을 섭취하면 원본 아이템도 없어지는걸 방지) 
            }
            else
            {
                slots[i] = new InventoryItem(); //빈칸을 저장
            }
        }
        isInit = true;
    }

    void Start()
    {
        InitSlotData();
        renderer = GetComponent<PanelRenderer>();
        renderer.RegisterUIReloadCallback(OnUIReload); //Renderer에 아래 만든 메서드를 주입, UI를 그릴 때 실행해줌
     
    }

    void OnRootPointUp(PointerUpEvent e)
    {
        if(!isDragging /* = isDragging == false*/)
        {
            return; //마우스가 때진 상태에서는 아무것도 하지 않음
        }

        isDragging = false; //마우스가 클릭 된 상태면 때진 상태이기 때문에 false로 바꿔줌.
        if(dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None; 
        }
        if(dragEndIndex >= 0 || dragEndIndex < totalSlotCount && dragEndIndex != dragStartIndex)
        {

        }
        dragStartIndex = -1;
    }

    void HandleItemDrop(int fromIndex, int toIndex)
    {
        InventoryItem fromItem = slots[fromIndex];
        InventoryItem toItem = slots[toIndex];
        if(toItem.IsEmpty() == true) //옮기는 곳이 빈곳인 경우
        {
            slots[toIndex] = fromItem.Clone(); //복사
            slots[fromIndex].Clear(); //기존 칸에 있는 아이템 지우기
            itemInfoLable.text = $"[{slots[toIndex].name}]아이템을 {toIndex + 1}로 이동했습니다!!";
        }
        else if(fromItem.IsSameItem(toItem) == true && toItem.maxStack > 1) //이름이 똑같고 소모품인 것을 드래그해서 합친 상황
        {
            int totalAmount = toItem.amount + fromItem.amount;
            if (totalAmount <= toItem.maxStack)
            {
                toItem.amount = totalAmount;
                slots[fromIndex].Clear();
                itemInfoLable.text = $"[{slots[toIndex].name}] 수량을 합쳤습니다! 현재수량: {toItem.amount}개!!";
            }
            else
            {
                toItem.amount = toItem.maxStack;
                fromItem.amount = totalAmount - fromItem.maxStack;
                itemInfoLable.text = $"[{slots[toIndex].name}] 최대 수량! {toItem.amount}개 와 {fromItem.amount}개로 나눴습니다!!";
            }
        }
        else
        {
            InventoryItem temp = slots[toIndex];
            slots[toIndex] = slots[fromIndex];
            slots[fromIndex] = temp;
            itemInfoLable.text = $"[{slots[toIndex].name}] 과 {slots[fromIndex].name}의 위치를 교환했습니다!!";
        }

        BuildInventorySlots();
    }

    //UIToolkit 사용시 필요한 method
    //PanelRenderer로 부터 내부 Element를 추출하기 위해 반드시 갖춰야 하는 규격 void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    //void타입의 메서드여야 하고 매개계변수로 PanelRenderer, VisualElement, int 세개의 값을 가져야 한다.
    //이름은 마음대로 지어도 된다.
    //1번 패러미터 PanelRenderer renderer: 함수를 실행시킨 Renderer가 해당 매개변수에 저장
    //2번 패러미터 VisualElement root: 함수를 실행시킨 Renderer가 가진 UXML파일의 root를 해당 매개변수에 저장
    //3번 패러미터 int version: Renderer가 몇 차례 다른 UXML을 변경해가며 그려야 할 경우 비교의 대상이 될 고유번호 지정
    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        this.root = root;
        slotContainer = root.Q<VisualElement>("SlotContainer");
        itemInfoLable = root.Q<Label>("ItemInfoLabel");
        CreateDragGhostIcon();
        root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        //root.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
        root.RegisterCallback<PointerUpEvent>(OnRootPointUp);
        BuildInventorySlots();
    }

    //고스트 아이콘 제작
    void CreateDragGhostIcon()
    {
        dragGhostIcon = new VisualElement();
        dragGhostIcon.name = "Ghost";

        dragGhostIcon.style.position = Position.Absolute;
        dragGhostIcon.style.width = 120;
        dragGhostIcon.style.height = 120;
        dragGhostIcon.style.opacity = 0.5f;
        //dragGhostIcon.style.backgroundColor = Color.red;

        dragGhostIcon.pickingMode = PickingMode.Ignore;
        dragGhostIcon.style.display = DisplayStyle.None;
        root.Add(dragGhostIcon);
        Debug.Log(dragGhostIcon);
    }

    void OnRootPointerMove(PointerMoveEvent e)
    {
        if(isDragging == false || dragGhostIcon == null)
        {
            return;
        }
        dragGhostIcon.style.left = e.position.x - 60;
        dragGhostIcon.style.top = e.position.y - 60;
    }

    void BuildInventorySlots()
    {
        if(slotContainer == null)
        {
            return;
        }

        slotContainer.Clear();  //VisualElement.Clear(): 자식Node를 제거하는 기능(원래 있던 기능)
                                //InventoryItem.Clear(): 객체 내부의 데이터를 0으로 변경(우리가 만듬)

        for (int i = 0; i < totalSlotCount; i++)
        {
            VisualElement slotElement = new VisualElement();
            slotElement.name = $"Slot_{i}";
            slotElement.userData = i;
            slotElement.AddToClassList("inventory-slot");
            slotElement.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            slotElement.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
            slotElement.RegisterCallback<PointerEnterEvent>(OnSlotPointerEnter);
            slotElement.RegisterCallback<PointerLeaveEvent>(OnSlotPointerLeave);

            InventoryItem currentItem = slots[i];
            if(currentItem.IsEmpty() == false)
            {
                //아이콘 표시 처리
                VisualElement iconElement = new VisualElement();
                iconElement.AddToClassList("slot-icon");
                iconElement.pickingMode = PickingMode.Ignore;// 아이콘을 클릭해도 마우스 가로채기 금지
                if(currentItem.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(currentItem.icon);
                }
                slotElement.Add(iconElement);

                //수량 표시 처리
                Label currentLabel = new Label("" + currentItem.amount);
                currentLabel.AddToClassList("slot-label");
                currentLabel.pickingMode = PickingMode.Ignore;//숫자를 클릭해도 마우스 가로채기 금지
                slotElement.Add(currentLabel);
            }
            slotContainer.Add(slotElement);
        }
    }

    //마우스 포인터가 특정 슬롯에 들어간 이벤트
    void OnSlotPointerEnter(PointerEnterEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot != null && slot.userData != null)
        {
            dragEndIndex = (int)slot.userData;
        }
    }

    void OnSlotPointerLeave(PointerLeaveEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot != null && slot.userData != null)
        {
           int index = (int)slot.userData;
            if (dragEndIndex == index)
            {
                dragEndIndex = -1;
            }
        }
    }

    void OnSlotPointerUp(PointerUpEvent e)
    {
        if (isDragging == false)
        {
            return;
        }

        isDragging = false;
        if(dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None;
        }
        //아이템 이동, 교체, 함침, 조합
        //슬롯에 아이템이 없을 때
        if(dragEndIndex >= 0 && dragEndIndex < totalSlotCount && dragEndIndex != dragStartIndex)
        {
            HandleItemDrop(dragStartIndex, dragEndIndex);
        }

        dragStartIndex = -1;
    }

    //인벤토리 클릭 이벤트
    void OnSlotPointerDown(PointerDownEvent e)
    {
        VisualElement clickedSlot = (VisualElement)e.currentTarget;

        if (clickedSlot == null || clickedSlot.userData == null)
        {
            return;
        }

        int slotIndex = (int)clickedSlot.userData;
        if (e.button == 0) //왼쪽 버튼이 눌렸을 때
        {
            //아이템 이름과 설명 표시 기능
            ShowItemInfo(slotIndex);
            //드래그 시작, 고스트 아이콘 등장
            if (slots[slotIndex].IsEmpty() == false)
            {
                isDragging = true;
                dragStartIndex = slotIndex;
                dragGhostIcon.style.backgroundImage = new StyleBackground(slots[slotIndex].icon);
                dragGhostIcon.style.display = DisplayStyle.Flex;
                dragGhostIcon.style.left = e.position.x - 60;
                dragGhostIcon.style.top = e.position.y - 60;
            }
        }
        else if (e.button == 1) //오른쪽 버튼이 눌렸을 때
        {
            //아이템 사용 기능
            UseItem(slotIndex);
            BuildInventorySlots();
        }
    }

    void ShowItemInfo(int index)
    {
        InventoryItem clickeditem = slots[index];
        if (clickeditem.IsEmpty() == true)
        {
            itemInfoLable.text = $"{index + 1}번 슬롯은 비어있습니다.";
        }
        else
        {
            itemInfoLable.text = $"[{clickeditem.name}] {clickeditem.description}";

        }
    }

    void UseItem(int index)
    {
        InventoryItem item = slots[index];
        if (item.IsEmpty() == true)
        {
            itemInfoLable.text = $"{(index + 1)}번 슬롯에는 사용할 아이템이 없습니다.";
            return;
        }
        if (item.maxStack <= 1)
        {
            itemInfoLable.text = $"{item.name}은/는 사용할 수 없습니다.";
            return;
        }

        string usedItem = item.name;
        item.amount -= 1;
        if(item.amount <= 0)
        {
            item.Clear(); //아이템 소멸
            itemInfoLable.text = $"{usedItem}을/를 모두 소모하였습니다.";
        }
        else
        {
            itemInfoLable.text = $"{usedItem}사용! 남은 수량 {item.amount}개.";
        }
    }
}

[Serializable]
public class InventoryItem
{
    public string name; //물약
    public string description; //체력을 50 회복시킨다.
    public Sprite icon; // 물약 모양
    public int amount; //물약 갯수 현재 5개
    public int maxStack; //최대 겹치기 최대 갯수 20개

    public InventoryItem()
    {
        Clear();
    }

    public InventoryItem(string a, string b, Sprite c, int d, int e)
    {
        name = a;
        description = b;
        icon = c;
        amount = d; 
        maxStack = e;
    }

    public void Clear()
    {
        name = "";
        description = "";
        icon = null;
        amount = 0;
        maxStack = 0;
    }

    public InventoryItem Clone()
    {
        return new InventoryItem(name, description, icon, amount, maxStack);
    }

    public bool IsEmpty() //지금 클릭된 좌표의 데이터가 빈칸인지 아닌지 판별
    {
        if (name == null || name == "" || amount == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsSameItem(InventoryItem other)
    {
        if (other == null)
        {
            return false;
        }
        if (this.IsEmpty() == true || other.IsEmpty() == true)
        {
            return false;
        }
        if (this.name ==  other.name)
        {
            return true;
        }
        else
        {
            return false; //다른 경우의 예외가 발생할 때 변경할 것
        }
    }
}
