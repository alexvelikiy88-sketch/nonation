using System;
using System.Collections.Generic;

namespace MIR
{
    // All amounts use integer minor credits, never floating-point money.
    // The prototype owns ONE local authoritative world. A shared online world needs a server transaction boundary.
    public sealed class MirEconomy
    {
        readonly MirWorldState s;
        readonly HashSet<string> seen = new HashSet<string>();
        float previousWorkPeriod = -1;
        public MirEconomy(MirWorldState state)
        {
            s = state;
            foreach (MirTransaction t in s.transactions) if (!String.IsNullOrEmpty(t.id)) seen.Add(t.id);
        }
        public int Balance(string id)
        {
            MirCitizen p=s.Citizen(id);if(p!=null)return p.cash;
            MirBusiness b=s.Business(id);if(b!=null)return b.cash;
            if(id=="city")return s.cityTreasury;
            if(id=="external")return s.externalMoney;
            return -1;
        }
        void Credit(string id,int delta)
        {
            MirCitizen p=s.Citizen(id);if(p!=null){p.cash+=delta;return;}
            MirBusiness b=s.Business(id);if(b!=null){b.cash+=delta;return;}
            if(id=="city")s.cityTreasury+=delta;
            if(id=="external")s.externalMoney+=delta;
        }
        public bool Transfer(string id,string from,string to,int amount,string reason)
        {
            if(String.IsNullOrEmpty(id) || String.IsNullOrEmpty(to) || String.IsNullOrEmpty(from) || amount<=0)return false;
            if(seen.Contains(id))return false;
            if(Balance(from)<amount || Balance(to)<0)return false;
            Credit(from,-amount);Credit(to,amount);
            var t=new MirTransaction{id=id,from=from,to=to,amount=amount,cause=reason,minute=s.gameMinutes};
            s.transactions.Add(t);seen.Add(id);return true;
        }
        public bool BuyFood(MirCitizen buyer, MirBusiness store)
        {
            if(buyer==null || store==null || store.stock<1 || store.price<1)return false;
            string id=s.NewId("purchase");
            if(!Transfer(id,buyer.id,store.id,store.price,"purchased: "+store.productId))return false;
            store.stock--; buyer.inventoryFood++;
            Log(buyer.name+" купил товар: "+store.productId);return true;
        }
        public bool Eat(MirCitizen person)
        {
            if(person==null || person.inventoryFood<=0)return false;
            person.inventoryFood--;
            person.satiety=Math.Min(100f,person.satiety+35f);
            person.mood=Math.Min(100f,person.mood+2f);
            Log(person.name+" поел из своего запаса.");return true;
        }
        public bool Work(MirCitizen worker)
        {
            if(worker==null || s.gameMinutes<worker.nextWorkMinute)return false;
            var employer=s.Business(worker.workplaceId);
            if(employer==null || employer.cash<1200 || worker.energy<25)return false;
            string id=s.NewId("salary");
            if(!Transfer(id,employer.id,worker.id,1200,"completed shift"))return false;
            worker.nextWorkMinute=s.gameMinutes+360f;
            worker.daysWorked++;worker.skill=Math.Min(999,worker.skill+1);
            worker.energy=Math.Max(0,worker.energy-9);
            if(employer.kind=="farm" && employer.stock+4<=employer.capacity && employer.seed>0 && employer.water>0)
            {
                employer.stock+=4;employer.seed--;employer.water--;
            }
            Log(worker.name+" закончил оплачиваемую работу.");return true;
        }
        public MirFreightOrder CreateRestock(string consumerId="market",string supplierId="warehouse",int qty=12)
        {
            if(s.ActiveFreight()!=null)return null;
            var from=s.Business(supplierId);var to=s.Business(consumerId);
            if(from==null || to==null || from.stock<qty || qty<1 || to.stock+qty>to.capacity)return null;
            int unitPrice=Math.Max(1,from.price); int cost=unitPrice*qty;int fee=qty*15;
            if(to.cash<cost+fee)return null;
            // Reserve goods and money now. Escrow is stored explicitly and paid only on physical completion.
            to.cash-=cost+fee;from.stock-=qty;
            var order=new MirFreightOrder{id="freight-"+s.jobSequence++,supplierId=supplierId,consumerId=consumerId,
                productId=from.productId,quantity=qty,escrow=cost,freightFee=fee,stockReserved=true,status=MirOrderStatus.Created};
            s.freight.Add(order);Log("Заказан реальный груз: "+qty+" ед. "+from.productId);return order;
        }
        public bool SetFreightStage(MirFreightOrder o,MirOrderStatus next)
        {
            if(o==null)return false;
            if(o.status==next)return false;
            bool okay= (o.status==MirOrderStatus.Created && next==MirOrderStatus.Loading)
                ||(o.status==MirOrderStatus.Loading && next==MirOrderStatus.Driving)
                ||(o.status==MirOrderStatus.Driving && next==MirOrderStatus.Unloading)
                ||(o.status==MirOrderStatus.Unloading && next==MirOrderStatus.Returning)
                ||(o.status==MirOrderStatus.Returning && next==MirOrderStatus.Completed);
            if(!okay)return false;
            if(next==MirOrderStatus.Unloading && !o.paid)
            {
                MirBusiness buyer=s.Business(o.consumerId),seller=s.Business(o.supplierId);
                if(buyer==null||seller==null||buyer.stock+o.quantity>buyer.capacity)return false;
                buyer.stock+=o.quantity;
                seller.cash+=o.escrow;
                s.cityTreasury+=o.freightFee;
                o.escrow=0;o.freightFee=0;o.paid=true;
                Log("Груз прибыл: "+buyer.name+" получил "+o.quantity+" ед.");
            }
            o.status=next;
            return true;
        }
        public bool CancelFreight(MirFreightOrder o)
        {
            if(o==null||o.paid||o.status==MirOrderStatus.Cancelled||o.status==MirOrderStatus.Completed)return false;
            MirBusiness supplier=s.Business(o.supplierId),buyer=s.Business(o.consumerId);
            if(supplier==null||buyer==null)return false;
            supplier.stock+=o.quantity;buyer.cash+=o.escrow+o.freightFee;
            o.escrow=0;o.freightFee=0;o.stockReserved=false;o.status=MirOrderStatus.Cancelled;
            Log("Грузовой заказ отменён, товар и деньги возвращены.");return true;
        }
        public void Tick(float minutes)
        {
            foreach(var citizen in s.citizens)
            {
                citizen.satiety=Math.Max(0,citizen.satiety-minutes*0.047f);
                citizen.hydration=Math.Max(0,citizen.hydration-minutes*0.078f);
                citizen.energy=Math.Max(0,citizen.energy-minutes*0.025f);
            }
            // Work is performed only for an assigned, present NPC via MirNpcAgent; never batch free wages here.
            // Limited real seed/water resources. No magical refilling of shops.
            int period=(int)Math.Floor(s.gameMinutes/360f);
            if(period!=(int)previousWorkPeriod)
            {
                previousWorkPeriod=period;
                var farm=s.Business("farm");
                var packing=s.Business("packing");
                if(farm!=null&&packing!=null&&farm.stock>=10 && packing.stock+8<=packing.capacity)
                {farm.stock-=10;packing.stock+=8;Log("Цех переработал 10 единиц сырья в 8 единиц продукции.");}
            }
        }
        public void Log(string msg)
        {
            s.events.Add(new MirEvent{minute=s.gameMinutes,text=msg});
            if(s.events.Count>80)s.events.RemoveRange(0,s.events.Count-80);
        }
        public int TotalKnownMoney()
        {
            int total=s.cityTreasury+s.externalMoney;
            foreach(var p in s.citizens)total+=p.cash;
            foreach(var b in s.businesses)total+=b.cash;
            foreach(var o in s.freight)total+=o.escrow+o.freightFee;
            return total;
        }
    }
}
